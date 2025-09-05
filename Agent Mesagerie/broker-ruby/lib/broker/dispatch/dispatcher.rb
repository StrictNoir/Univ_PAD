# lib/broker/dispatch/dispatcher.rb
# frozen_string_literal: true

require 'securerandom'
require 'concurrent'

module Broker
  module Dispatch
    class Dispatcher
      attr_writer :registry

      def initialize(registry:, workers:, queue_size:, redis: nil)
        @registry = registry
        @q = SizedQueue.new(queue_size)
        @pool = Concurrent::FixedThreadPool.new(workers)
        @redis = redis
        workers.times { @pool.post { loop { deliver(@q.pop) } } }
      end

      def enqueue(message)
        @redis&.persist!(message)
        @q << message
      rescue ThreadError
        # queue full
      end

      def deliver(msg)
        payload = { 'op' => 'DELIVER', 'deliveryId' => SecureRandom.uuid, 'message' => msg }
        @registry.targets_for(msg['type']).each do |conn|
          conn.send_json!(payload)
        rescue StandardError
          # optional: drop or log
        rescue StandardError => e
          warn "deliver_failed: #{e.message}"
        end
      end
    end
  end
end
