# frozen_string_literal: true

require 'json'
require 'net/http'
require 'securerandom'
require 'uri'

module SmartProxy
  module Tools
    # Automates the SmartProxy employee conflict scenario using Ruby's Net::HTTP.
    # It mirrors the behaviour of the original C# console tool while offering
    # better testability for the new Ruby-based test harness.
    class HttpClientDemo
      DEFAULT_SERVER1_URL = 'http://localhost:8080/api/Employee'
      DEFAULT_SERVER2_URL = 'http://localhost:8081/api/Employee'
      DEFAULT_SYNC_WAIT = 3
      DEFAULT_RESOLUTION_WAIT = 5

      Result = Struct.new(:employee_id, :server1, :server2, keyword_init: true) do
        def consistent?
          return false unless server1 && server2

          server1.slice('FirstName', 'LastName', 'Email') ==
            server2.slice('FirstName', 'LastName', 'Email')
        end
      end

      attr_reader :server1_url, :server2_url, :sync_wait_seconds, :resolution_wait_seconds

      def initialize(
        server1_url: ENV.fetch('SMART_PROXY_SERVER1_URL', DEFAULT_SERVER1_URL),
        server2_url: ENV.fetch('SMART_PROXY_SERVER2_URL', DEFAULT_SERVER2_URL),
        sync_wait_seconds: ENV.fetch('SMART_PROXY_WAIT_FOR_SYNC', DEFAULT_SYNC_WAIT).to_i,
        resolution_wait_seconds: ENV.fetch('SMART_PROXY_WAIT_FOR_RESOLUTION', DEFAULT_RESOLUTION_WAIT).to_i,
        sleeper: ->(seconds) { sleep(seconds) },
        logger: $stdout
      )
        @server1_url = server1_url
        @server2_url = server2_url
        @sync_wait_seconds = sync_wait_seconds
        @resolution_wait_seconds = resolution_wait_seconds
        @sleeper = sleeper
        @logger = logger
      end

      def perform_conflict_test
        log_heading 'Employee Conflict Resolution Tester'

        log_step 1, 'Creating initial employee on Server1'
        employee_id = create_employee(server1_url, 'John', 'Doe', "john.doe+#{SecureRandom.hex(3)}@example.com")
        raise 'Failed to create employee on Server1' if employee_id.to_s.empty?

        log " Employee created with ID: #{employee_id}\n"

        log_step 2, "Waiting #{sync_wait_seconds} seconds for sync between servers"
        wait(sync_wait_seconds)

        log_step 3, 'Verifying employee exists on both servers'
        employee1 = get_employee(server1_url, employee_id)
        employee2 = get_employee(server2_url, employee_id)

        unless employee1 && employee2
          raise 'Employee missing on one of the servers after initial sync'
        end

        log_server_state('Server1', employee1)
        log_server_state('Server2', employee2)

        log_heading 'CONFLICT TEST'
        log_step 4, 'Sending concurrent updates to BOTH servers'

        threads = []
        threads << Thread.new { update_employee(server1_url, employee_id, 'Jane', 'Smith', 'jane.smith@example.com', 'Server1') }
        threads << Thread.new { update_employee(server2_url, employee_id, 'Jack', 'Johnson', 'jack.johnson@example.com', 'Server2') }
        threads.each(&:join)

        log_step 5, "Waiting #{resolution_wait_seconds} seconds for conflict resolution"
        wait(resolution_wait_seconds)

        log_heading 'FINAL STATE'
        final1 = get_employee(server1_url, employee_id)
        final2 = get_employee(server2_url, employee_id)

        log_final_state('Server1', final1)
        log_final_state('Server2', final2)

        result = Result.new(employee_id: employee_id, server1: final1, server2: final2)

        if result.consistent?
          log '\n✓ SUCCESS: Both servers have consistent data!'
          log "  Winner: #{final1['FirstName']} #{final1['LastName']}"
        else
          log '\n✗ CONFLICT: Servers have inconsistent data!'
          log '  This indicates a conflict resolution problem.'
        end

        result
      end

      def create_employee(base_url, first_name, last_name, email)
        payload = {
          FirstName: first_name,
          LastName: last_name,
          Email: email
        }

        response = request(:post, URI.join(base_url + '/', 'add'), payload)
        response.body.to_s.delete_prefix('"').delete_suffix('"')
      end

      def get_employee(base_url, id)
        response = request(:get, URI.join(base_url + '/', id.to_s))
        return nil unless response.is_a?(Net::HTTPSuccess)

        JSON.parse(response.body)
      end

      def update_employee(base_url, id, first_name, last_name, email, server_name)
        payload = {
          FirstName: first_name,
          LastName: last_name,
          Email: email
        }

        start_time = Time.now
        log "[#{start_time.strftime('%H:%M:%S.%L')}] #{server_name}: Sending update to #{first_name} #{last_name}..."
        response = request(:put, URI.join(base_url + '/', "update/#{id}"), payload)
        end_time = Time.now
        duration = ((end_time - start_time) * 1000).round

        if response.is_a?(Net::HTTPSuccess)
          log "[#{end_time.strftime('%H:%M:%S.%L')}] #{server_name}: ✓ Update successful (#{duration}ms)"
          true
        else
          log "[#{end_time.strftime('%H:%M:%S.%L')}] #{server_name}: ✗ Update failed - #{response.code}"
          false
        end
      end

      private

      def wait(seconds)
        @sleeper.call(seconds)
      end

      def log(message)
        @logger.puts(message)
      end

      def log_heading(title)
        log("\n=== #{title} ===\n")
      end

      def log_step(number, description)
        log("Step #{number}: #{description}...")
      end

      def log_server_state(name, employee)
        log(" #{name}: #{employee['FirstName']} #{employee['LastName']} (LastChanged: #{employee['LastChangedAt']})")
      end

      def log_final_state(name, employee)
        return log(" #{name}: employee not found") unless employee

        log("\n#{name} Final State:")
        log("  Name: #{employee['FirstName']} #{employee['LastName']}")
        log("  Email: #{employee['Email']}")
        log("  LastChanged: #{employee['LastChangedAt']}")
      end

      def request(method, uri, payload = nil)
        http = Net::HTTP.new(uri.host, uri.port)
        http.use_ssl = uri.scheme == 'https'

        request = case method
                  when :get
                    Net::HTTP::Get.new(uri)
                  when :post
                    Net::HTTP::Post.new(uri)
                  when :put
                    Net::HTTP::Put.new(uri)
                  when :delete
                    Net::HTTP::Delete.new(uri)
                  else
                    raise ArgumentError, "Unsupported HTTP method: #{method}"
                  end

        if payload
          request['Content-Type'] = 'application/json'
          request.body = JSON.dump(payload)
        end

        http.request(request)
      end
    end
  end
end
