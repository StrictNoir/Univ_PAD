# lib/broker/protocol/validator.rb
# frozen_string_literal: true

module Broker
  module Protocol
    module Validator
      REQUIRED = %w[topic payload].freeze

      module_function

      def valid?(msg)
        return false unless msg.is_a?(Hash)

        miss = REQUIRED - msg.keys.map(&:to_s)
        return false unless miss.empty?
        true
      end
    end
  end
end
