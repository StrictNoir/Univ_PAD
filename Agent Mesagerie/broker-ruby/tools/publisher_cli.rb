#!/usr/bin/env ruby
# frozen_string_literal: true

# tools/publisher_cli.rb
# Interactive CLI for publishing messages over the gRPC interface.

require 'grpc'
require 'socket'
require 'time'

$LOAD_PATH.unshift(File.expand_path('../lib', __dir__))

require 'broker_pb'
require 'broker_services_pb'

def usage!
  abort("usage: #{$PROGRAM_NAME} HOST PORT")
end

host, port = ARGV
usage! unless host && port

def prefer_ipv4_host(host)
  return host if host.nil?

  normalized = host.to_s.strip
  return normalized if normalized.empty?
  return normalized if normalized =~ /\A\d{1,3}(?:\.\d{1,3}){3}\z/

  Addrinfo.foreach(normalized, nil, Socket::AF_INET, Socket::SOCK_STREAM) do |info|
    return info.ip_address if info.ip_address
  end

  normalized
rescue SocketError
  normalized
end

ipv4_host = prefer_ipv4_host(host)
endpoint = "#{ipv4_host}:#{port}"
stub = Broker::Proto::Broker::Stub.new(endpoint, :this_channel_is_insecure)

if ipv4_host != host
  puts "Resolved #{host} to IPv4 #{ipv4_host}"
end

puts "Connecting to broker at #{endpoint}"
puts 'Type "exit" at any prompt to quit.'

trap('INT') do
  puts "\nExiting..."
  exit
end

def read_input(prompt)
  print prompt
  line = $stdin.gets
  return unless line

  line = line.encode('UTF-8', invalid: :replace, undef: :replace, replace: '')
  line.strip
rescue EncodingError => e
  STDERR.puts "ERROR #{e.class}: #{e.message}"
  nil
end

loop do
  subject = read_input('subject> ')
  break unless subject

  break if subject.casecmp('exit').zero?
  next if subject.empty?

  message = read_input('message> ')
  break unless message

  break if message.casecmp('exit').zero?
  next if message.empty?

  payload = message.encode('UTF-8')
  payload = payload.b if payload.respond_to?(:b)

  envelope = Broker::Proto::Envelope.new(
    subject: subject,
    payload: payload,
    timestamp_ms: (Time.now.utc.to_f * 1000).to_i
  )

  begin
    ack = stub.publish(envelope)
    detail = ack.detail.to_s.strip
    puts "PUBLISHED subject=#{subject} message_id=#{ack.message_id} accepted=#{ack.accepted}"
    puts "  detail: #{detail}" unless detail.empty?
  rescue GRPC::BadStatus => e
    STDERR.puts "ERROR #{e.code}: #{e.details}"
  rescue StandardError => e
    STDERR.puts "ERROR #{e.class}: #{e.message}"
  end
end

puts 'Goodbye!'