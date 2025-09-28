# frozen_string_literal: true

module Broker
  module Grpc
    # Thread-safe buffer for delivering envelopes to a streaming gRPC client.
    class SubscriberStream
      CLOSE_TOKEN = Object.new

      def initialize(subject:, consumer_group:, buffer_size:, logger:)
        @subject = subject
        @consumer_group = consumer_group
        @buffer_size = buffer_size
        @logger = logger
        @queue = Queue.new
        @closed = false
        @mutex = Mutex.new
      end

      def push(message)
        @mutex.synchronize do
          return if @closed

          if @queue.size >= @buffer_size
            dropped = begin
              @queue.pop(true)
            rescue StandardError
              nil
            end
            @logger.warn("subscriber_buffer_full subject=#{@subject} consumer_group=#{@consumer_group} dropped=#{!dropped.nil?}")
          end

          @queue << message unless @closed
        end
      end

      def enumerator
        @enumerator ||= Enumerator.new do |yielder|
          loop do
            item = @queue.pop
            break if item.equal?(CLOSE_TOKEN)

            yielder << item
          end
        end
      end

      def close
        @mutex.synchronize do
          return if @closed

          @closed = true
          @queue << CLOSE_TOKEN
        end
      end

      def closed?
        @mutex.synchronize { @closed }
      end
    end
  end
end
