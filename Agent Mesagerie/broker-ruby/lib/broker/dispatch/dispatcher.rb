# lib/broker/dispatch/dispatcher.rb
# frozen_string_literal: true

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
        @delivery_seq = Concurrent::AtomicFixnum.new
        workers.times { @pool.post { loop { deliver(@q.pop) } } }
      end

      def enqueue(message)
        if @store
          id = @store.persist!(message)
          message['__id'] = id if id
        end
        @q << message
      rescue ThreadError
        # full
      end

      def next_delivery_id
        @delivery_seq.increment
      end

      def deliver(msg)
        payload = {
          'op' => 'DELIVER',
          'deliveryId' => next_delivery_id,
          'topic' => msg['topic'],
          'message' => msg['payload'],
          'timestamp' => msg['timestamp']
        }
        payload['storeId'] = msg['__id'] if msg['__id']
        @registry.targets_for(msg['topic']).each do |conn|
          conn.send_json!(payload)
          puts "DELIVER topic=#{msg['topic']} payload=#{msg['payload'].inspect}"
        rescue ::Broker::Connection::BackpressureError => e
          warn "deliver_failed: #{e.message}"
        rescue StandardError => e
          warn "deliver_failed: #{e.message}"
          @registry.remove(conn)
          conn.close
        end
      end
    end
  end
end
