# frozen_string_literal: true

# tools/publisher_cli.rb
# !/usr/bin/env ruby
require 'socket'
require 'json'
require 'securerandom'
require 'time'
def wr(io, obj)
  s = JSON.dump(obj)
  io.write([s.bytesize].pack('N'))
  io.write(s)
end
host, port, subj = ARGV
s = TCPSocket.new(host, port.to_i)
msg = { 'id' => SecureRandom.uuid, 'type' => subj, 'payload' => { 'demo' => true },
        'timestamp' => Time.now.utc.iso8601 }
wr(s, { 'op' => 'PUBLISH', 'message' => msg })
puts "SENT: #{msg}"
