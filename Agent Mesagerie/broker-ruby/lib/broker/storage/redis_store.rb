# lib/broker/storage/redis_store.rb
# frozen_string_literal: true

require 'set'
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

  def pending_for(subject, subscriber_id: nil)
    key = key_for(subject)
    raw = @r.hgetall(key)
    acked_ids = acked_ids_for(subject, subscriber_id)

    raw.values.map do |json|
      decoded = Broker::Json::LOAD.call(json)
      record = MessageRecord.new(
        subject: decoded['subject'],
        message_id: decoded['message_id'],
        payload: decoded['payload'],
        headers: decoded['headers'] || {},
        timestamp_ms: decoded['timestamp_ms']
      )
      next if subscriber_id && acked_ids.include?(record.message_id)

      record
    end.compact.sort_by(&:timestamp_ms)
  rescue StandardError => e
    warn "redis_pending_failed: #{e.message}"
    []
  end

  def ack!(subject, message_id, subscriber_id)
    return false if subscriber_id.to_s.strip.empty?

    key = key_for(subject)
    exists = @r.hexists(key, message_id)
    return false unless exists

    @r.sadd(ack_key_for(subject, subscriber_id), message_id)
    true
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

  def ack_key_for(subject, subscriber_id)
    "#{@prefix}#{subject}:ack:#{subscriber_id}"
  end

  def acked_ids_for(subject, subscriber_id)
    return Set.new unless subscriber_id

    ids = @r.smembers(ack_key_for(subject, subscriber_id))
    Set.new(ids)
  rescue StandardError => e
    warn "redis_acked_ids_failed: #{e.message}"
    Set.new
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
