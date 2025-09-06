# lib/broker/protocol/validator.rb
# frozen_string_literal: true

require 'time'
module Broker
  module Protocol
    module Validator
      REQUIRED = %w[id type payload timestamp].freeze

      module_function

      def valid?(msg)
        return false unless msg.is_a?(Hash)

        miss = REQUIRED - msg.keys.map(&:to_s)
        return false unless miss.empty?

        begin
          Time.iso8601(msg['timestamp'])
        rescue StandardError
          (return false)
        end
        true
      end
    end
  end
end
