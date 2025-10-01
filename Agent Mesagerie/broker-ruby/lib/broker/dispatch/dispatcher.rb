# lib/broker/dispatch/dispatcher.rb
# frozen_string_literal: true

require 'concurrent'
require 'logger'
require 'broker_services_pb'

module Broker
  module Dispatch
    class Dispatcher
      attr_writer :registry

      STOP = Object.new

      def initialize(registry:, workers:, queue_size:, logger: Logger.new($stdout))
        @registry = registry
        @logger = logger
        @queue = SizedQueue.new(queue_size)
        @pool = Concurrent::FixedThreadPool.new(workers)
        @worker_count = workers
        workers.times { @pool.post { worker_loop } }
      end

      def enqueue(record)
        @queue.push(record)
      rescue ThreadError
        @logger.warn('dispatch_queue_full dropping message')
      end

      def deliver_to(record, subscribers)
        subscribers = Array(subscribers).compact
        return if subscribers.empty?

        envelope = build_envelope(record)

        subscribers.each do |subscriber|
          subscriber.push(envelope)
          @logger.debug("deliver subject=#{record.subject} message_id=#{record.message_id}")
        rescue StandardError => e
          @logger.warn("deliver_failed subject=#{record.subject} message_id=#{record.message_id} error=#{e.message}")
        end
      end


      def shutdown
        @worker_count.times { @queue << STOP }
        @pool.shutdown
        @pool.wait_for_termination
      end

      private

      def worker_loop
        loop do
          record = @queue.pop
          break if record.equal?(STOP)

          deliver(record)
        end
      rescue StandardError => e
        @logger.error("dispatch_worker_error error=#{e.message}")
        retry
      end

      def deliver(record)
        subscribers = @registry.subscribers_for(record.subject)
        if subscribers.empty?
          @logger.info("no_subscribers subject=#{record.subject}")
          return
        end

        deliver_to(record, subscribers)
      end

      def build_envelope(record)
        ::Broker::Proto::Envelope.new(
          subject: record.subject,
          payload: record.payload,
          headers: record.headers || {},
          message_id: record.message_id,
          timestamp_ms: record.timestamp_ms
        )
      end
    end
  end
end
