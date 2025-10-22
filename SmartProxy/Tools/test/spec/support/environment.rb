# frozen_string_literal: true

module SmartProxy
  module Test
    module Environment
      module_function

      def server_urls
        {
          primary: ENV.fetch('SMART_PROXY_SERVER1_URL', 'http://localhost:8080/api/Employee'),
          secondary: ENV.fetch('SMART_PROXY_SERVER2_URL', 'http://localhost:8081/api/Employee')
        }
      end

      def proxy_url
        ENV.fetch('SMART_PROXY_PROXY_URL', 'http://localhost:9000/api/Employee')
      end

      def wait_for_sync_seconds
        ENV.fetch('SMART_PROXY_WAIT_FOR_SYNC', '3').to_i
      end

      def wait_for_resolution_seconds
        ENV.fetch('SMART_PROXY_WAIT_FOR_RESOLUTION', '5').to_i
      end
    end
  end
end
