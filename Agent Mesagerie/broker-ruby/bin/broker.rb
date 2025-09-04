# bin/broker
# !/usr/bin/env ruby
# frozen_string_literal: true

$LOAD_PATH.unshift(File.expand_path('../lib', __dir__))
require 'broker/version'
require 'socket'
puts "Broker #{Broker::VERSION} starting… (code comes next commits)"
