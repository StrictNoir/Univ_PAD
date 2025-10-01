# frozen_string_literal: true

require 'logger'
require 'time'
require 'broker_services_pb'
require_relative 'subscription_registry'
require_relative 'subscriber_stream'
require_relative '../dispatch/dispatcher'

module Broker
  module Grpc
    class Service < ::Broker::Proto::Broker::Service
      def initialize(store:, dispatcher:, registry:, validator: nil, subscriber_buffer_size: 100,
                     logger: Logger.new($stdout))
        @store = store
        @dispatcher = dispatcher
        @registry = registry
        @validator = validator
        @subscriber_buffer_size = subscriber_buffer_size
        @logger = logger
      end

      def publish(envelope, call)
        record = nil
        subject = envelope.subject.to_s.strip
        @logger.info("publisher_connected subject=#{subject.empty? ? '<empty>' : subject} peer=#{peer_info(call)}")
        record = build_record(envelope)
        validate_record!(record)
        unless @store.respond_to?(:persist!)
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::FAILED_PRECONDITION,
                                                       'store not configured')
        end

        stored = @store.persist!(record)
        @dispatcher.enqueue(stored)
        @logger.info("publish subject=#{stored.subject} message_id=#{stored.message_id}")
        ::Broker::Proto::PublishAck.new(message_id: stored.message_id, accepted: true)
      rescue GRPC::BadStatus => e
        raise e
      rescue StandardError => e
        @logger.error("publish_failed error=#{e.message}")
        raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INTERNAL, 'internal server error')
      ensure
        @logger.info("publisher_disconnected subject=#{record_subject(record)} peer=#{peer_info(call)}")
      end

      def subscribe(subscription, call)
        subject = subscription.subject.to_s.strip
        if subject.empty?
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                       'subject is required')
        end

        subscriber_id = subscription.subscriber_id.to_s.strip
        if subscriber_id.empty?
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                       'subscriber_id is required')
        end

        stream = SubscriberStream.new(subject: subject,
                                      subscriber_id: subscriber_id,
                                      buffer_size: @subscriber_buffer_size,
                                      logger: @logger)
        @registry.register(subject, stream)
        @logger.info("subscriber_connected subject=#{subject} subscriber_id=#{subscriber_id} peer=#{peer_info(call)}")
        enqueue_pending(subject, subscriber_id, stream)
        enum = stream.enumerator
        cancel_monitor = start_cancel_monitor(call, stream)

        Enumerator.new do |y|
          loop do
            y << enum.next
          end
        rescue StopIteration
          # stream closed
        ensure
          cancel_monitor&.kill
          @registry.unregister(subject, stream)
          stream.close
          @logger.info("subscriber_disconnected subject=#{subject} subscriber_id=#{subscriber_id} peer=#{peer_info(call)}")
        end
      rescue GRPC::BadStatus => e
        raise e
      rescue StandardError => e
        @logger.error("subscribe_failed subject=#{subscription&.subject} error=#{e.message}")
        raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INTERNAL, 'internal server error')
      end

      def ack(request, _call)
        subject = request.subject.to_s.strip
        message_id = request.message_id.to_s.strip
        subscriber_id = request.subscriber_id.to_s.strip
        if subject.empty? || message_id.empty? || subscriber_id.empty?
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                       'subject, message_id, and subscriber_id are required')
        end

        unless @store.respond_to?(:ack!)
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::UNIMPLEMENTED,
                                                       'ack not supported by store')
        end

        acknowledged = @store.ack!(subject, message_id, subscriber_id)
        @logger.info("ack subject=#{subject} subscriber_id=#{subscriber_id} message_id=#{message_id} acknowledged=#{acknowledged}")
        ::Broker::Proto::AckReply.new(acknowledged: acknowledged)
      rescue GRPC::BadStatus => e
        raise e
      rescue StandardError => e
        @logger.error("ack_failed subject=#{request&.subject} subscriber_id=#{request&.subscriber_id} message_id=#{request&.message_id} error=#{e.message}")
        raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INTERNAL, 'internal server error')
      end

      private

      def build_record(envelope)
        {
          subject: envelope.subject.to_s.strip,
          payload: envelope.payload,
          headers: envelope.headers.respond_to?(:to_h) ? envelope.headers.to_h : (envelope.headers || {}),
          message_id: envelope.message_id.to_s.strip,
          timestamp_ms: envelope.timestamp_ms.positive? ? envelope.timestamp_ms : current_time_ms
        }
      end

      def current_time_ms
        (Time.now.utc.to_f * 1000).to_i
      end

      def enqueue_pending(subject, subscriber_id, stream)
        return unless @store.respond_to?(:pending_for)

        Array(@store.pending_for(subject, subscriber_id: subscriber_id)).each do |record|
          @dispatcher.deliver_to(record, stream)
        end
      end

      def start_cancel_monitor(call, stream)
        Thread.new do
          loop do
            break if stream.closed?

            if call.cancelled?
              stream.close
              break
            end
            sleep 0.1
          end
        end
      end


      def validate_record!(record)
        subject = record[:subject].to_s.strip
        if subject.empty?
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                       'subject is required')
        end

        record[:subject] = subject

        if record[:payload].nil? || record[:payload].bytesize.zero?
          raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                       'payload is required')
        end
        return unless @validator

        valid = @validator.call(record)
        return if valid

        raise ::GRPC::BadStatus.new_status_exception(::GRPC::Core::StatusCodes::INVALID_ARGUMENT,
                                                     'message validation failed')
      end

      def peer_info(call)
        call&.peer || 'unknown'
      rescue StandardError
        'unknown'
      end

      def record_subject(record)
        return '<unknown>' unless record

        value = record[:subject] || (record.respond_to?(:subject) ? record.subject : nil)
        str = value.to_s.strip
        str.empty? ? '<empty>' : str
      end
    end
  end
end
