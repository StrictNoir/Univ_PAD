# lib/broker/protocol/validator.rb
# frozen_string_literal: true

module Broker
  module Protocol
    module Validator
      module_function

      def valid?(msg)
        return false unless msg.respond_to?(:[]) || msg.respond_to?(:subject)

        subject = value_for(msg, :subject)
        payload = value_for(msg, :payload)
        !(subject.nil? || subject.to_s.strip.empty? || payload.nil? || payload.to_s.empty?)
      end

      def value_for(obj, key)
        if obj.respond_to?(key)
          obj.public_send(key)
        elsif obj.respond_to?(:[])
          obj[key] || obj[key.to_s]
        end
      end
    end
  end
end
