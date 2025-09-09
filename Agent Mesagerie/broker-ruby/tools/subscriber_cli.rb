# frozen_string_literal: true

# tools/subscriber_cli.rb
# !/usr/bin/env ruby
require 'socket'
require 'fileutils'
require 'json'
def wr(io, obj)
  s = JSON.dump(obj)
  io.write([s.bytesize].pack('N'))
  io.write(s)
end

def rd(io)
  hdr = io.read(4)
  return nil unless hdr
  len = hdr.unpack1('N')
  buf = +''
  buf << io.readpartial(len - buf.bytesize) while buf.bytesize < len
  buf
rescue IOError
  nil
end

def ckpt_path(host, port, topic)
  File.join(Dir.home, '.broker_checkpoints', "#{host}_#{port}_#{topic}.ckpt")
end

def load_checkpoint(host, port, topic)
  path = ckpt_path(host, port, topic)
  File.read(path).strip if File.exist?(path)
end

def save_checkpoint(host, port, topic, id)
  dir = File.join(Dir.home, '.broker_checkpoints')
  FileUtils.mkdir_p(dir)
  File.write(ckpt_path(host, port, topic), id)
end
host, port = ARGV
abort("usage: #{$PROGRAM_NAME} HOST PORT") unless host && port

s = TCPSocket.new(host, port.to_i)
puts 'Enter topics to subscribe to (blank line to finish):'
loop do
  print 'topic> '
  t = $stdin.gets
  break unless t

  t = t.strip
  break if t.empty?

  req = { 'op' => 'SUBSCRIBE', 'topic' => t }
  if (from = load_checkpoint(host, port, t))
    req['from'] = from
  end
  wr(s, req)
  puts "Subscribed to #{t}"
end
trap('INT') do
  s.close
  exit
end

reader = Thread.new do
  loop do
    (raw = rd(s)) or break
    begin
      msg = JSON.parse(raw)
      if msg['op'] == 'DELIVER'
        save_checkpoint(host, port, msg['topic'], msg['storeId']) if msg['storeId']
        puts "#{msg['topic']}: #{msg['message']}"
      else
        puts "IN: #{raw}"
      end
    rescue JSON::ParserError
      puts "IN: #{raw}"
    end
  end
end

puts 'Type a topic name to subscribe or "exit" to quit:'
loop do
  print 'topic> '
  line = $stdin.gets
  break unless line

  line = line.strip
  break if line.casecmp('exit').zero?
  next if line.empty?

  req = { 'op' => 'SUBSCRIBE', 'topic' => line }
  if (from = load_checkpoint(host, port, line))
    req['from'] = from
  end
  wr(s, req)
  puts "Subscribed to #{line}"
end

s.close
reader.join
