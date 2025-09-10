# lib/broker/storage/in_memory_store.rb
# frozen_string_literal: true

require 'set'


class InMemoryStore
  def initialize
    @messages = Hash.new { |h, k| h[k] = [] }
    @topics = Set.new
    @m = Mutex.new
  end

  # Persist a message in memory grouped by topic
  def persist!(message)
    topic = message['topic']
    return unless topic

    @m.synchronize do
      arr = @messages[topic]
      arr << message
      @topics << topic
      arr.length
    end
  end

  # Retrieve all messages for a given topic
  def messages_for(topic)
    @m.synchronize { @messages[topic].dup }
  end

  def topic_exists?(topic)
    @m.synchronize { @topics.include?(topic) }
  end

  def create_topic(topic)
    @m.synchronize do
      @messages[topic] ||= []
      @topics << topic
    end
  end

  def replay_topic(topic, from_id: '0')
    from = from_id.to_i
    @m.synchronize do
      Array(@messages[topic]).each_with_index do |msg, idx|
        id = idx + 1
        yield id.to_s, msg if id > from
      end
    end
  end

  # No-op helpers to satisfy storage interface used by broker
  def load_checkpoint(_path)
    {}
  end

  def save_checkpoint(_path, _hash); end
end
