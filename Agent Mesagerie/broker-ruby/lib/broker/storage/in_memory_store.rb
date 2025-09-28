# lib/broker/storage/in_memory_store.rb
# frozen_string_literal: true

class InMemoryStore
  MessageRecord = Struct.new(:subject, :message_id, :payload, :headers, :timestamp_ms, keyword_init: true)

  def initialize
    @messages = Hash.new { |h, k| h[k] = {} }
    @lock = Mutex.new
    @counters = Hash.new(0)
  end

  def persist!(record)
    @lock.synchronize do
      subject = record[:subject].to_s
      message_id = record[:message_id].to_s.strip

      if message_id.empty?
        @counters[subject] += 1
        message_id = @counters[subject].to_s
      else
        begin
          numeric = Integer(message_id)
          @counters[subject] = [@counters[subject], numeric].max
        rescue ArgumentError, TypeError
          # non-numeric ids do not update the counter
        end
      end

      msg = MessageRecord.new(subject: subject,
                              message_id: message_id,
                              payload: record[:payload],
                              headers: record[:headers] || {},
                              timestamp_ms: record[:timestamp_ms])
      @messages[subject][message_id] = msg
      msg
    end
  end

  def pending_for(subject)
    @lock.synchronize do
      @messages[subject].values.map(&:dup)
    end
  end

  def ack!(subject, message_id)
    @lock.synchronize do
      !!@messages[subject].delete(message_id)
    end
  end

  def reset!
    @lock.synchronize do
      @messages.clear
      @counters.clear
    end
  end
end
