# frozen_string_literal: true

# tools/subscriber_cli.rb
# !/usr/bin/env ruby
require 'socket'
require 'json'
def wr(io, obj)
  s = JSON.dump(obj)
  io.write([s.bytesize].pack('N'))
  io.write(s)
end

def rd(io)
  len = io.read(4)&.unpack1('N')
  return nil unless len

  io.read(len)
end
host, port, pat, sid = ARGV
s = TCPSocket.new(host, port.to_i)
wr(s, { 'op' => 'SUBSCRIBE', 'subjects' => [pat], 'subscriberId' => sid || "sub-#{rand(1000)}" })
trap('INT') do
  s.close
  exit
end

reader = Thread.new do
  loop do
    (raw = rd(s)) or break
    puts "IN: #{raw}"
  end
end
loop do
  (line = STDIN.gets) or break
  break if line.strip.casecmp('exit').zero?
end

s.close
reader.join
