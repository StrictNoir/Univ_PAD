#!/usr/bin/env ruby
# frozen_string_literal: true

# tools/subscriber_cli.rb
# Interactive CLI for subscribing to messages over the gRPC interface.

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

def read_input(prompt = nil)
  print prompt if prompt
  line = $stdin.gets
  return unless line

  line = line.encode('UTF-8', invalid: :replace, undef: :replace, replace: '')
  line.strip
rescue EncodingError => e
  warn "ERROR #{e.class}: #{e.message}"
  nil
end

puts 'Type "exit" at any prompt to quit.'

def exit_command?(input)
  input&.casecmp('exit')&.zero?
end

def print_help
  puts <<~HELP
    Commands:
      add <subject>    Start subscribing to <subject>.
      remove <subject> Stop subscribing to <subject>. Use "remove all" to stop all.
      list             Show currently active subscriptions.
      help             Show this help message.
      exit             Disconnect and exit.
  HELP
end

def stop_subscription(subject, active_calls, active_calls_mutex, entry: nil, verbose: true)
  entry ||= active_calls_mutex.synchronize { active_calls[subject] }

  unless entry
    warn "Not subscribed to #{subject.inspect}." if verbose
    return
  end

  call = entry[:call]
  if call.respond_to?(:cancel)
    begin
      call.cancel
    rescue StandardError => e
      warn "Failed to cancel subscription for #{subject.inspect}: #{e.message}"
    end
  end

  thread = entry[:thread]
  if thread && !thread.join(5)
    warn "Subscription thread for #{subject.inspect} did not terminate within 5 seconds; killing."
    thread.kill
    thread.join
  end

  active_calls_mutex.synchronize { active_calls.delete(subject) }
  puts "Unsubscribed from #{subject.inspect}" if verbose
end

def stop_all_subscriptions(active_calls, active_calls_mutex, verbose: false)
  entries = []
  active_calls_mutex.synchronize do
    active_calls.each do |subject, entry|
      entries << [subject, entry]
    end
  end

  entries.each do |subject, entry|
    stop_subscription(subject, active_calls, active_calls_mutex, entry: entry, verbose: verbose)
  end
end

def start_subscription(subject, stub, auto_ack, active_calls, active_calls_mutex)
  subject = subject.to_s.strip
  if subject.empty?
    warn 'Subject cannot be empty.'
    return
  end

  entry = { subject: subject, thread: nil, call: nil }

  already_subscribed = false
  active_calls_mutex.synchronize do
    if active_calls.key?(subject)
      already_subscribed = true
    else
      active_calls[subject] = entry
    end
  end

  if already_subscribed
    warn "Already subscribed to #{subject.inspect}."
    return
  end

  begin
    thread = Thread.new do
      Thread.current.name = "sub:#{subject}" if Thread.current.respond_to?(:name=)

      subscription = Broker::Proto::Subscription.new(
        subject: subject,
        consumer_group: consumer_group
      )

      begin
        operation = stub.subscribe(subscription, return_op: true)
        active_calls_mutex.synchronize { entry[:call] = operation }

        stream = operation.execute

        stream.each do |envelope|
          payload = envelope.payload.dup
          payload = payload.force_encoding('UTF-8') if payload.respond_to?(:force_encoding)
          puts "RECEIVED subject=#{envelope.subject} message_id=#{envelope.message_id} timestamp_ms=#{envelope.timestamp_ms}"
          if payload.empty?
            puts '  (empty payload)'
          else
            puts "  payload: #{payload}"
          end

          headers = envelope.headers
          headers = headers.to_h if headers.respond_to?(:to_h)
          if headers && (!headers.respond_to?(:empty?) || !headers.empty?)
            puts '  headers:'
            headers.each do |key, value|
              puts "    #{key}: #{value}"
            end
          end

          next unless auto_ack
          next if envelope.message_id.to_s.strip.empty?

          begin
            reply = stub.ack(
              Broker::Proto::AckRequest.new(
                subject: envelope.subject,
                message_id: envelope.message_id
              )
            )
            puts "  acked: #{reply.acknowledged}"
          rescue GRPC::BadStatus => e
            warn "  ACK ERROR #{e.code}: #{e.details}"
          rescue StandardError => e
            warn "  ACK ERROR #{e.class}: #{e.message}"
          end
        end
      rescue GRPC::BadStatus => e
        warn "ERROR #{e.code}: #{e.details}" unless e.code == GRPC::Core::StatusCodes::CANCELLED
      rescue StandardError => e
        warn "ERROR #{e.class}: #{e.message}"
      ensure
        active_calls_mutex.synchronize { active_calls.delete(subject) }
        begin
          operation&.cancel if defined?(operation) && operation.respond_to?(:cancel)
        rescue StandardError
          # ignore cancellation failures during shutdown
        end
        puts "Subscription for #{subject.inspect} ended."
      end
    end
  rescue StandardError => e
    active_calls_mutex.synchronize { active_calls.delete(subject) }
    warn "Failed to start subscription for #{subject.inspect}: #{e.message}"
    return
  end

  active_calls_mutex.synchronize { entry[:thread] = thread }
  puts "Subscribed to #{subject.inspect}"
end

subjects_input = read_input('subjects (comma separated)> ')
if exit_command?(subjects_input)
  puts 'Goodbye!'
  exit
end

if subjects_input.nil?
  warn 'No subjects provided.'
  exit(1)
end

subjects = subjects_input.tr(',', ' ').split.map(&:strip).reject(&:empty?).uniq

auto_ack_input = read_input('auto-ack? [y/N]> ')
if exit_command?(auto_ack_input)
  puts 'Goodbye!'
  exit
end
auto_ack = auto_ack_input&.casecmp('y')&.zero?

active_calls_mutex = Mutex.new
active_calls = {}
stop_mutex = Mutex.new
stop_requested = false

request_stop = lambda do
  already_requested = false
  stop_mutex.synchronize do
    already_requested = stop_requested
    stop_requested = true
  end
  return if already_requested

  puts "\nDisconnecting..."
  stop_all_subscriptions(active_calls, active_calls_mutex, verbose: false)
end

Signal.trap('INT') { request_stop.call }

if subjects.empty?
  puts 'No initial subjects specified.'
else
  puts "Initial subjects: #{subjects.map(&:inspect).join(', ')}"
  subjects.each do |subject|
    start_subscription(subject, stub, auto_ack, active_calls, active_calls_mutex)
  end
end

puts 'Type "help" for a list of commands.'

command_thread = Thread.new do
  prompt_pending = true

  loop do
    break if stop_mutex.synchronize { stop_requested }

    if prompt_pending
      print 'command> '
      $stdout.flush
      prompt_pending = false
    end

    ready = IO.select([$stdin], nil, nil, 0.5)
    next unless ready

    line = $stdin.gets
    if line.nil?
      request_stop.call
      break
    end

    input = line.encode('UTF-8', invalid: :replace, undef: :replace, replace: '').strip
    prompt_pending = true
    next if input.empty?

    parts = input.split(' ', 2)
    command = parts[0].downcase
    argument = parts[1]&.strip

    case command
    when 'exit'
      request_stop.call
      break
    when 'help'
      print_help
    when 'list'
      current_subjects = active_calls_mutex.synchronize { active_calls.keys }
      if current_subjects.empty?
        puts 'No active subscriptions.'
      else
        puts "Active subscriptions: #{current_subjects.join(', ')}"
      end
    when 'add'
      if argument.nil? || argument.empty?
        warn 'Subject cannot be empty.'
      else
        start_subscription(argument, stub, auto_ack, active_calls, active_calls_mutex)
      end
    when 'remove'
      if argument.nil? || argument.empty?
        warn 'Subject cannot be empty.'
      elsif argument.casecmp('all').zero?
        stop_all_subscriptions(active_calls, active_calls_mutex, verbose: true)
      else
        stop_subscription(argument, active_calls, active_calls_mutex)
      end
    else
      puts 'Unknown command. Type "help" for a list of commands.'
    end
  end

  request_stop.call
end

command_thread.join
request_stop.call

puts 'Disconnected.'
