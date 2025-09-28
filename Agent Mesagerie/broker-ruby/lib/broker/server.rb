# frozen_string_literal: true

require 'grpc'
require 'logger'
require 'socket'
require 'broker_services_pb'
require_relative 'grpc/service'
require_relative 'grpc/subscription_registry'
require_relative 'dispatch/dispatcher'
require_relative 'protocol/validator'

module Broker
  class Server
    def initialize(host:, port:, worker_count:, dispatch_queue_size:, subscriber_buffer_size:, store:,
                   logger: Logger.new($stdout))
      @host = host
      @bind_host = prefer_ipv4_host(host)
      @port = port
      @logger = logger
      @registry = Broker::Grpc::SubscriptionRegistry.new
      @dispatcher = Broker::Dispatch::Dispatcher.new(registry: @registry,
                                                     workers: worker_count,
                                                     queue_size: dispatch_queue_size,
                                                     logger: @logger)
      @service = Broker::Grpc::Service.new(store: store,
                                           dispatcher: @dispatcher,
                                           registry: @registry,
                                           validator: method(:validate_record),
                                           subscriber_buffer_size: subscriber_buffer_size,
                                           logger: @logger)
      @server = GRPC::RpcServer.new(pool_size: worker_count, max_waiting_requests: dispatch_queue_size)
      @server.add_http2_port("#{@bind_host}:#{@port}", :this_port_is_insecure)
      @server.handle(@service)
    end

    def start
      log_host = @bind_host == @host ? @host : "#{@host}(#{@bind_host})"
      @logger.info("broker_grpc_listening host=#{log_host} port=#{@port}")
      trap_signals
      @server.run_till_terminated
    ensure
      stop
    end

    def stop
      @logger.info('broker_grpc_stopping')
      @server&.stop
      @dispatcher&.shutdown
    end

    private

    def prefer_ipv4_host(host)
      return host if host.nil?

      string_host = host.to_s.strip
      return string_host if string_host.empty?

      return string_host if string_host =~ /\A\d{1,3}(?:\.\d{1,3}){3}\z/

      Addrinfo.foreach(string_host, nil, Socket::AF_INET, Socket::SOCK_STREAM) do |info|
        return info.ip_address if info.ip_address
      end

      string_host
    rescue SocketError
      string_host
    end

    def trap_signals
      %w[INT TERM].each do |sig|
        Signal.trap(sig) { stop }
      rescue ArgumentError
        # Signal isn't supported
      end
    end

    def validate_record(record)
      Broker::Protocol::Validator.valid?(record)
    end
  end
end
