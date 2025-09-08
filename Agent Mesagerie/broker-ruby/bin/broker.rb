#!/usr/bin/env ruby
# frozen_string_literal: true
# Agent Mesagerie/broker-ruby/bin/broker.rb

$LOAD_PATH.unshift(File.expand_path('../lib', __dir__))
require 'yaml'
require 'broker/version'
require 'broker/server'
require 'broker/dispatch/dispatcher'

cfg_path = File.expand_path('../config/broker.yml', __dir__)
cfg = File.exist?(cfg_path) ? YAML.load_file(cfg_path) : {}
host = ENV['BROKER_HOST'] || cfg['host'] || '0.0.0.0'
port = Integer(ENV['BROKER_PORT'] || cfg['port'] || 5001)
workers = Integer(ENV['BROKER_WORKERS'] || cfg['worker_count'] || 4)
dq = Integer(ENV['BROKER_DISPATCH_QUEUE_SIZE'] || cfg['dispatch_queue_size'] || 10_000)
sq = Integer(ENV['BROKER_SEND_QUEUE_SIZE'] || cfg['send_queue_size'] || 1_000)
maxb = Integer(ENV['BROKER_MAX_FRAME_BYTES'] || cfg['max_frame_bytes'] || 1_048_576)

redis_url = ENV['REDIS_URL']
redis_prefix = ENV['REDIS_STREAM_PREFIX'] || 'stream:messages:'
store = nil
if redis_url
  require 'broker/storage/redis_store'
  store = RedisStore.new(url: redis_url, prefix: redis_prefix)
end
dispatcher = Broker::Dispatch::Dispatcher.new(registry: nil, workers: workers, queue_size: dq, store: store)

replay_cfg = cfg['replay'] || {}
if store && replay_cfg['enabled']
  cp_path = replay_cfg['checkpoint'] || 'data/redis_checkpoint.yml'
  require 'fileutils'
  FileUtils.mkdir_p(File.dirname(cp_path))
  data = store.load_checkpoint(cp_path)
  Array(replay_cfg['subjects']).each do |subj|
    last = data[subj] || '0-0'
    store.replay_subject(subj, from_id: last) do |id, msg|
      dispatcher.enqueue(msg)
      last = id
    end
    data[subj] = last
  end
  store.save_checkpoint(cp_path, data)
end

server = Broker::Server.new(host: host, port: port, max_frame: maxb, send_queue_size: sq, dispatcher: dispatcher)
server.start
