# frozen_string_literal: true

# tools/publisher_cli.rb
# !/usr/bin/env ruby
require 'socket'
require 'json'
require 'time'
def wr(io, obj)
  s = JSON.dump(obj)
  io.write([s.bytesize].pack('N'))
  io.write(s)
end
host, port, subj = ARGV
s = TCPSocket.new(host, port.to_i)
trap('INT') do
  s.close
  exit
end

id = 0
loop do
  print '> '
  line = $stdin.gets
  break unless line

  line = line.strip
  break if line.casecmp('exit').zero?
  next if line.empty?

  id += 1
  msg = {
    'id' => id,
    'type' => subj,
    'payload' => { 'text' => line },
    'timestamp' => Time.now.utc.iso8601
  }
  wr(s, { 'op' => 'PUBLISH', 'message' => msg })
  puts "SENT: #{msg}"
end

s.close
