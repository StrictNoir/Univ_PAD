# lib/broker/connection.rb
# frozen_string_literal: true

require 'securerandom'
require_relative 'json'
require_relative 'framing'

module Broker
  class Connection
    attr_reader :id

    def initialize(socket, send_queue_size:)
      @socket = socket
      @id = SecureRandom.hex(4)
      @out = SizedQueue.new(send_queue_size)
      @writer = Thread.new { writer_loop }
    end

    def send_json!(obj)
      data = Json::DUMP.call(obj)
      raise :backpressure if @out.size >= @out.max

      @out << data
    end

    def read_frame(max:)
      Framing.read_frame(@socket, max: max)
    end

    def close
      @writer&.kill
      begin
        @socket.close
      rescue StandardError
        nil
      end
    end

    private

    def writer_loop
      loop do
        payload = @out.pop
        ok = Framing.write_frame(@socket, payload)
        break unless ok
      end
    rescue StandardError
      # swallow, close on ensure
    ensure
      begin
        @socket.close
      rescue StandardError
        nil
      end
    end
  end
end
