#!/usr/bin/env ruby
# frozen_string_literal: true

# Agent Mesagerie/broker-ruby/bin/broker.rb

$LOAD_PATH.unshift(File.expand_path('../lib', __dir__))
require 'yaml'
require 'broker/version'
require 'broker/server'

cfg_path = File.expand_path('../config/broker.yml', __dir__)
cfg = File.exist?(cfg_path) ? YAML.load_file(cfg_path) : {}
host = ENV.fetch('BROKER_HOST', cfg['host'] || '0.0.0.0')
port = Integer(ENV.fetch('BROKER_PORT', cfg['port'] || 5001))
workers = Integer(ENV.fetch('BROKER_WORKERS', cfg['worker_count'] || 4))
dq = Integer(ENV.fetch('BROKER_DISPATCH_QUEUE_SIZE', cfg['dispatch_queue_size'] || 10_000))
subscriber_buffer = Integer(ENV.fetch('BROKER_SUBSCRIBER_BUFFER', cfg['subscriber_buffer'] || 100))

redis_url = ENV['REDIS_URL']
redis_prefix = ENV['REDIS_STREAM_PREFIX'] || 'stream:messages:'
store = nil
if redis_url
  require 'broker/storage/redis_store'
  store = RedisStore.new(url: redis_url, prefix: redis_prefix)
else
  require 'broker/storage/in_memory_store'
  store = InMemoryStore.new
end
server = Broker::Server.new(host: host,
                            port: port,
                            worker_count: workers,
                            dispatch_queue_size: dq,
                            subscriber_buffer_size: subscriber_buffer,
                            store: store)
server.start