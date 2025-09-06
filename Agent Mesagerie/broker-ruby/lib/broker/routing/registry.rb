# lib/broker/routing/registry.rb
# frozen_string_literal: true

require_relative 'matcher'

module Broker
  module Routing
    class Registry
      def initialize
        @conns = {} # conn_id => Connection
        @subs  = {} # conn_id => [patterns]
        @by_subscriber = {} # subscriberId => conn_id
        @m = Mutex.new
      end

      def add(conn)
        @m.synchronize do
          @conns[conn.id] = conn
          @subs[conn.id] = []
        end
      end

      def remove(conn)
        @m.synchronize do
          @conns.delete(conn.id)
          @subs.delete(conn.id)
        end
      end

      def update(conn, patterns, subscriber_id: nil)
        @m.synchronize do
          @subs[conn.id] = patterns
          @by_subscriber[subscriber_id] = conn.id if subscriber_id
        end
      end

      def subscriber_id_for_conn(conn)
        @m.synchronize { @by_subscriber.key(conn.id) }
      end

      def targets_for(subject)
        ids = @m.synchronize do
          @subs.select { |_id, pats| pats.any? { |pat| Matcher.match?(subject, pat) } }.keys
        end
        ids.map { |id| @conns[id] }.compact
      end

      def stats
        @m.synchronize { { connections: @conns.size, subscribers: @subs.count { |_, p| !p.empty? } } }
      end
    end
  end
end
