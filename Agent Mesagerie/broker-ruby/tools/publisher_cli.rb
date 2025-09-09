# frozen_string_literal: true

# tools/publisher_cli.rb
# !/usr/bin/env ruby
require 'socket'
require 'json'
def wr(io, obj)
  s = JSON.dump(obj)
  io.write([s.bytesize].pack('N'))
  io.write(s)
end

host, port = ARGV
abort("usage: #{$PROGRAM_NAME} HOST PORT") unless host && port

s = TCPSocket.new(host, port.to_i)
trap('INT') do
  s.close
  exit
end

loop do
  print 'topic> '
  topic = $stdin.gets
  break unless topic

  topic = topic.strip
  break if topic.casecmp('exit').zero?
  next if topic.empty?

  print 'message> '
  line = $stdin.gets
  break unless line

  line = line.strip
  break if line.casecmp('exit').zero?
  next if line.empty?

  wr(s, { 'op' => 'PUBLISH', 'topic' => topic, 'message' => { 'text' => line } })
  puts "SENT to #{topic}: #{line}"
end

s.close
