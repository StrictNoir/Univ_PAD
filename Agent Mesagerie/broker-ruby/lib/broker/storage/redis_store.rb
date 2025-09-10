# lib/broker/storage/redis_store.rb
# frozen_string_literal: true

require 'time'
require 'yaml'
require_relative '../json'
class RedisStore
  def initialize(url:, prefix:)
    require 'redis'
    @r = Redis.new(url: url)
    @prefix = prefix
  end

  def persist!(message)
    key = "#{@prefix}#{message['topic']}"
    @r.xadd(key, { 'data' => Broker::Json::DUMP.call(message), 'ts' => Time.now.utc.iso8601 })
  rescue StandardError => e
    warn "redis_persist_failed: #{e.message}"
    nil
  end

  def persist_conn!(conn_id, payload)
    key = "stream:out:#{conn_id}"
    @r.xadd(key, { 'data' => Broker::Json::DUMP.call(payload), 'ts' => Time.now.utc.iso8601 })
  rescue StandardError => e
    warn "redis_persist_conn_failed: #{e.message}"
  end

  def load_checkpoint(path)
    File.exist?(path) ? YAML.load_file(path) : {}
  end

  def save_checkpoint(path, hash)
    require 'fileutils'
    FileUtils.mkdir_p(File.dirname(path))
    File.write(path, YAML.dump(hash))
  end

  def topic_exists?(topic)
    key = "#{@prefix}#{topic}"
    @r.exists(key).positive?
  rescue StandardError => e
    warn "redis_topic_exists_failed: #{e.message}"
    false
  end

  # Redis streams are created automatically on first publish
  # so subscriptions to new topics do not require pre-creation.
  def create_topic(_topic)
    # no-op
  end

  def replay_topic(subject, from_id:)
    key = "#{@prefix}#{subject}"
    @r.xrange(key, "(#{from_id}", '+').each do |id, fields|
      msg = Broker::Json::LOAD.call(fields['data'])
      yield id, msg
    end
  end
end
