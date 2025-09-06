# lib/broker/dispatch/dispatcher.rb
# frozen_string_literal: true

require 'securerandom'
require 'concurrent'

module Broker
  module Dispatch
    class Dispatcher
      attr_writer :registry

      def initialize(registry:, workers:, queue_size:, store: nil)
        @registry = registry
        @store = store
        @q = SizedQueue.new(queue_size)
        @pool = Concurrent::FixedThreadPool.new(workers)
        workers.times { @pool.post { loop { deliver(@q.pop) } } }
      end

      def enqueue(message)
        @store&.persist!(message)
        @q << message
      rescue ThreadError
        # full
      end

      def deliver(msg)
        payload = { 'op' => 'DELIVER', 'deliveryId' => SecureRandom.uuid, 'message' => msg }
        @registry.targets_for(msg['type']).each do |conn|
          conn.send_json!(payload)
        rescue StandardError => e
          if e == :backpressure || e&.message == 'backpressure'
            @store&.persist_conn!(conn.id, payload)
            warn 'backpressure_spill'
          else
            warn "deliver_failed: #{e.message}"
          end
        end
      end
    end
  end
end
