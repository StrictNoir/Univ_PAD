# lib/broker/server.rb
# frozen_string_literal: true

require 'socket'
require_relative 'connection'
require_relative 'routing/registry'
require_relative 'json'
require_relative 'protocol/validator'

module Broker
  class Server
    def initialize(host:, port:, max_frame:, send_queue_size:, dispatcher:)
      @host = host
      @port = port
      @max_frame = max_frame
      @send_q = send_queue_size
      @dispatcher = dispatcher
      @registry = Routing::Registry.new
      @dispatcher.registry = @registry
      @srv = TCPServer.new(@host, @port)
      trap('INT') { stop }
      trap('TERM') { stop }
    end

    def start
      puts "Listening on #{@host}:#{@port}"
      loop do
        sock = @srv.accept
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
          pats = Array(obj['subjects']).select { |s| s.is_a?(String) && !s.empty? }
          if pats.empty?
            conn.send_json!({ 'op' => 'ERROR', 'code' => 'BadRequest', 'detail' => 'subjects required' })
          else
            @registry.update(conn, pats)
            conn.send_json!({ 'op' => 'SUBSCRIBED', 'subjects' => pats })
          end
        when 'PING'
          conn.send_json!({ 'op' => 'PONG' })
        when 'PUBLISH'
          msg = obj['message']
          if Broker::Protocol::Validator.valid?(msg)
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
