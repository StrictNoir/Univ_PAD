# lib/broker/routing/registry.rb
# frozen_string_literal: true

require_relative 'matcher'

module Broker
  module Routing
    class Registry
      def initialize
        @conns = {} # conn_id => Connection
        @subs  = {} # conn_id => [patterns]
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

      def update(conn, patterns)
        @m.synchronize do
          current = @subs[conn.id] || []
          @subs[conn.id] = (current | patterns)
        end
      end

      def subscribed?(conn, pattern)
        @m.synchronize do
          Array(@subs[conn.id]).include?(pattern)
        end
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
