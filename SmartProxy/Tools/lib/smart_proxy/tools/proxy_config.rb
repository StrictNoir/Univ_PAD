# frozen_string_literal: true

require 'json'

module SmartProxy
  module Tools
    # Lightweight loader for Ocelot-style configuration files used by SmartProxy.
    class ProxyConfig
      class Route
        attr_reader :upstream_methods, :upstream_path, :downstream_path,
                    :downstream_scheme, :downstream_hosts

        def initialize(route_hash)
          @upstream_methods = Array(route_hash['UpstreamHttpMethod'])
          @upstream_path = route_hash['UpstreamPathTemplate']
          @downstream_path = route_hash['DownstreamPathTemplate']
          @downstream_scheme = route_hash['DownstreamScheme']
          @downstream_hosts = Array(route_hash['DownstreamHostAndPorts']).map do |host|
            { 'Host' => host['Host'], 'Port' => host['Port'].to_i }
          end
          @load_balancer = route_hash.fetch('LoadBalancerOptions', {})
        end

        def round_robin?
          @load_balancer['Type'].to_s.casecmp('roundrobin').zero?
        end

        def to_h
          {
            upstream_methods: upstream_methods,
            upstream_path: upstream_path,
            downstream_path: downstream_path,
            downstream_scheme: downstream_scheme,
            downstream_hosts: downstream_hosts,
            round_robin: round_robin?
          }
        end
      end

      attr_reader :routes

      def self.load(path)
        new(JSON.parse(File.read(path, encoding: 'bom|utf-8')))
      end

      def initialize(config_hash)
        @routes = Array(config_hash['Routes']).map { |route| Route.new(route) }
      end

      def summary
        routes.map(&:to_h)
      end

      def round_robin_for?(method)
        routes.any? do |route|
          route.upstream_methods.map(&:upcase).include?(method.to_s.upcase) && route.round_robin?
        end
      end
    end
  end
end
