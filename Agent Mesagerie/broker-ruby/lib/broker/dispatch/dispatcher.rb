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
        @store&.persist!(message)
        @q << message
      rescue ThreadError
        # full
      end

      def deliver(msg)
        payload = { 'op' => 'DELIVER', 'deliveryId' => @delivery_seq.increment, 'message' => msg }
        @registry.targets_for(msg['type']).each do |conn|
          sid = @registry.subscriber_id_for_conn(conn)
          conn.send_json!(payload)
          puts "DELIVER message_id=#{msg['id']} to=#{sid} payload=#{msg['payload'].inspect}" if sid
        rescue BackpressureError => e
          if e == :backpressure || e&.message == 'backpressure'
            sid ||= @registry.subscriber_id_for_conn(conn)
            @store&.persist_conn!(sid, payload) if sid
            warn 'backpressure_spill'
          else
            warn "deliver_failed: #{e.message}"
          end
        end
      end
    end
  end
end
