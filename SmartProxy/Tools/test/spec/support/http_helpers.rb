# frozen_string_literal: true

require 'json'
require 'net/http'
require 'uri'

module SmartProxy
  module Test
    module HttpHelpers
      module_function

      def json_request(method:, url:, body: nil, headers: {})
        uri = URI(url)
        request = net_http_request_for(method).new(uri)
        request['Content-Type'] = 'application/json'
        headers.each { |key, value| request[key] = value }
        request.body = body.to_json if body

        perform_request(uri, request)
      end

      def perform_request(uri, request, timeout: 5)
        Net::HTTP.start(uri.host, uri.port, open_timeout: timeout, read_timeout: timeout, use_ssl: uri.scheme == 'https') do |http|
          http.request(request)
        end
      end

      def parse_json(body)
        JSON.parse(body)
      rescue JSON::ParserError
        body
      end

      def reachable?(url, method: :get)
        uri = URI(url)
        request = net_http_request_for(method).new(uri)
        perform_request(uri, request, timeout: 1)
        true
      rescue StandardError
        false
      end

      def delete_if_exists(base_url, id)
        url = join_url(base_url, "delete/#{id}")
        response = json_request(method: :delete, url: url)
        response.is_a?(Net::HTTPSuccess) || response.code.to_i == 404
      rescue StandardError
        false
      end

      def join_url(base_url, path)
        base = base_url.end_with?('/') ? base_url : base_url + '/'
        base + path
      end

      def net_http_request_for(method)
        case method.to_s.downcase
        when 'get' then Net::HTTP::Get
        when 'post' then Net::HTTP::Post
        when 'put' then Net::HTTP::Put
        when 'delete' then Net::HTTP::Delete
        else
          raise ArgumentError, "Unsupported method: #{method}"
        end
      end
    end
  end
end
