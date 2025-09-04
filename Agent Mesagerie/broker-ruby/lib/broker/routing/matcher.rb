# lib/broker/routing/matcher.rb
# frozen_string_literal: true

module Broker
  module Routing
    module Matcher
      module_function

      # "order.*" matches "order.created"; '*' = exact one segment
      def match?(subject, pattern)
        s = subject.split('.')
        p = pattern.split('.')
        return false unless s.length == p.length

        p.zip(s).all? { |pp, ss| pp == '*' || pp == ss }
      end
    end
  end
end
