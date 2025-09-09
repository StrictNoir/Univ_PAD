# lib/broker/server.rb
# frozen_string_literal: true

require 'socket'
require_relative 'connection'
require_relative 'routing/registry'
require_relative 'json'
require_relative 'protocol/validator'
require 'time'

module Broker
  class Server
    def initialize(host:, port:, max_frame:, send_queue_size:, dispatcher:, store: nil)
      @host = host
      @port = port
      @max_frame = max_frame
      @send_q = send_queue_size
      @dispatcher = dispatcher
      @store = store
      @registry = Routing::Registry.new
      @dispatcher.registry = @registry
      @srv = TCPServer.new(@host, @port)
      @port = @srv.addr[1]
      trap('INT') { stop }
      trap('TERM') { stop }
    end

    def start
      puts "Listening on #{@host}:#{@srv.addr[1]}"
      loop do
        sock = @srv.accept
        puts "ACCEPT #{sock.peeraddr[2]}:#{sock.peeraddr[1]}"
        Thread.new { handle(sock) }
      end
    rescue IOError, SystemCallError
      # shutdown
    end

    def handle(sock)
      conn = Connection.new(sock, send_queue_size: @send_q)
      @registry.add(conn)
      while (raw = conn.read_frame(max: @max_frame))
        obj = begin
          Json::LOAD.call(raw)
        rescue StandardError
          nil
        end
        if obj.nil?
          conn.send_json!({ 'op' => 'ERROR', 'code' => 'BadRequest', 'detail' => 'invalid JSON' })
          next
        end
        case obj['op']
        when 'SUBSCRIBE'
          topic = obj['topic'].to_s.strip
          from = obj['from']
          if topic.empty?
            conn.send_json!({ 'op' => 'ERROR', 'code' => 'BadRequest', 'detail' => 'topic required' })
          elsif @store.respond_to?(:topic_exists?) && !@store.topic_exists?(topic)
            conn.send_json!({ 'op' => 'ERROR', 'code' => 'NotFound', 'detail' => 'unknown topic' })
          else
            @registry.update(conn, [topic])
            conn.send_json!({ 'op' => 'SUBSCRIBED', 'topic' => topic })
            puts "SUBSCRIBE topic=#{topic}"
            if @store.respond_to?(:replay_topic)
              @store.replay_topic(topic, from_id: from || '0') do |id, msg|
                payload = {
                  'op' => 'DELIVER',
                  'deliveryId' => @dispatcher.next_delivery_id,
                  'topic' => topic,
                  'message' => msg['payload'],
                  'timestamp' => msg['timestamp'],
                  'storeId' => id
                }
                conn.send_json!(payload)
              end
            elsif @store.respond_to?(:messages_for)
              @store.messages_for(topic)&.each do |msg|
                conn.send_json!({ 'op' => 'DELIVER', 'topic' => topic, 'message' => msg['payload'],
                                  'timestamp' => msg['timestamp'] })
              end
            end
          end

        when 'PING'
          conn.send_json!({ 'op' => 'PONG' })
        when 'PUBLISH'
          topic = obj['topic'].to_s.strip
          payload = obj['message']
          msg = { 'topic' => topic, 'payload' => payload, 'timestamp' => Time.now.utc.iso8601 }
          if Broker::Protocol::Validator.valid?(msg)
            puts "PUBLISH topic=#{topic} payload=#{payload.inspect}"
            @dispatcher.enqueue(msg)
          else
            conn.send_json!({ 'op' => 'ERROR', 'code' => 'BadRequest', 'detail' => 'invalid message' })
          end
        else
          conn.send_json!({ 'op' => 'ERROR', 'code' => 'BadRequest', 'detail' => 'unknown op' })
        end
      end
    rescue StandardError => e
      warn "conn_error: #{e.message}"
    ensure
      @registry.remove(conn) if conn
      conn&.close
    end

    def stop
      puts 'Shutting down…'
      s = @registry.stats
      puts "connections=#{s[:connections]} subscribers=#{s[:subscribers]}"
      begin
        @srv.close
      rescue StandardError
        nil
      end
      exit
    end
  end
end
