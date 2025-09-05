# lib/broker/storage/redis_store.rb
# frozen_string_literal: true

require 'time'
require_relative '../json'
class RedisStore
  def initialize(url:, prefix:)
    require 'redis'
    @r = Redis.new(url: url)
    @prefix = prefix
  end

  def persist!(message)
    key = "#{@prefix}#{message['type']}"
    @r.xadd(key, { 'data' => Broker::Json::DUMP.call(message), 'ts' => Time.now.utc.iso8601 })
  rescue StandardError => e
    warn "redis_persist_failed: #{e.message}"
  end
end
