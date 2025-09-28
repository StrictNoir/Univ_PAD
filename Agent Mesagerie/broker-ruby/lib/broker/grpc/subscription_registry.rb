# frozen_string_literal: true

require 'concurrent'

module Broker
  module Grpc
    # Thread-safe registry of subscribers grouped by subject/topic.
    class SubscriptionRegistry
      def initialize
        @subjects = Concurrent::Map.new
      end

      def register(subject, subscriber)
        subject = subject.to_s
        list = @subjects.compute_if_absent(subject) { Concurrent::Array.new }
        list << subscriber
        subscriber
      end

      def unregister(subject, subscriber)
        list = @subjects[subject.to_s]
        return unless list

        list.delete(subscriber)
        @subjects.delete(subject.to_s) if list.empty?
      end

      def subscribers_for(subject)
        Array(@subjects[subject.to_s] || [])
      end

      def stats
        subs = @subjects.values.reduce(0) { |sum, arr| sum + arr.length }
        { subjects: @subjects.keys.length, subscribers: subs }
      end
    end
  end
end
