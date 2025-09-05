# bin/broker (update)
# !/usr/bin/env ruby
# frozen_string_literal: true

$LOAD_PATH.unshift(File.expand_path('../lib', __dir__))
require 'yaml'
require 'broker/version'
require 'broker/server'
require 'broker/dispatch/dispatcher'

cfg = YAML.load_file(File.expand_path('../config/broker.yml', __dir__))
host = ENV['BROKER_HOST'] || cfg['host']
port = Integer(ENV['BROKER_PORT'] || cfg['port'])
workers = Integer(ENV['BROKER_WORKERS'] || cfg['worker_count'])
dq = Integer(ENV['BROKER_DISPATCH_QUEUE_SIZE'] || cfg['dispatch_queue_size'])
sq = Integer(ENV['BROKER_SEND_QUEUE_SIZE'] || cfg['send_queue_size'])
maxb = Integer(ENV['BROKER_MAX_FRAME_BYTES'] || cfg['max_frame_bytes'])

redis_url = ENV['REDIS_URL']
redis_prefix = ENV['REDIS_STREAM_PREFIX'] || 'stream:messages:'
store = nil
if redis_url
  require 'broker/storage/redis_store'
  store = RedisStore.new(url: redis_url, prefix: redis_prefix)
end
dispatcher = Broker::Dispatch::Dispatcher.new(registry: nil, workers: workers, queue_size: dq, store: store)
# registry va fi creat în Server; dispatcher nu are nevoie aici de el

server = Broker::Server.new(host: host, port: port, max_frame: maxb, send_queue_size: sq, dispatcher: dispatcher)
server.start
