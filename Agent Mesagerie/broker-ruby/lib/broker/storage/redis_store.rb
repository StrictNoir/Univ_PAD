# lib/broker/storage/redis_store.rb
# frozen_string_literal: true

require 'time'
require 'yaml'
require_relative '../json'
class RedisStore
  MessageRecord = Struct.new(:subject, :message_id, :payload, :headers, :timestamp_ms, keyword_init: true)

  def initialize(url:, prefix:)
    require 'redis'
    @r = Redis.new(url: url)
    @prefix = prefix
  end

  def persist!(record)
    normalized = nil
    normalized = normalize_record(record)
    key = key_for(normalized[:subject])
    data = Broker::Json::DUMP.call(normalized)
    @r.hset(key, normalized[:message_id], data)
    MessageRecord.new(normalized)
  rescue StandardError => e
    warn "redis_persist_failed: #{e.message}"
    MessageRecord.new(normalized || record)
  end

  def pending_for(subject)
    key = key_for(subject)
    raw = @r.hgetall(key)
    raw.values.map do |json|
      decoded = Broker::Json::LOAD.call(json)
      MessageRecord.new(
        subject: decoded['subject'],
        message_id: decoded['message_id'],
        payload: decoded['payload'],
        headers: decoded['headers'] || {},
        timestamp_ms: decoded['timestamp_ms']
      )
    end.sort_by(&:timestamp_ms)
  rescue StandardError => e
    warn "redis_pending_failed: #{e.message}"
    []
  end

  def ack!(subject, message_id)
    key = key_for(subject)
    @r.hdel(key, message_id) == 1
  rescue StandardError => e
    warn "redis_ack_failed: #{e.message}"
    false
  end

  private

  def normalize_record(record)
    subject = record[:subject].to_s
    message_id = record[:message_id].to_s.strip
    message_id = next_message_id_for(subject) if message_id.empty?

    {
      subject: subject,
      payload: record[:payload],
      headers: record[:headers] || {},
      message_id: message_id,
      timestamp_ms: record[:timestamp_ms]
    }
  end

  def key_for(subject)
    "#{@prefix}#{subject}"
  end

  def counter_key_for(subject)
    "#{@prefix}#{subject}:seq"
  end

  def next_message_id_for(subject)
    @r.incr(counter_key_for(subject)).to_s
  rescue StandardError => e
    warn "redis_sequence_failed: #{e.message}"
    Time.now.utc.to_i.to_s
  end
end
