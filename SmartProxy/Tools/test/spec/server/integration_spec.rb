# frozen_string_literal: true

require 'securerandom'
require_relative '../spec_helper'

module SmartProxy
  module Test
    module ServerExamples
      include SmartProxy::Test::HttpHelpers

      def create_employee(base_url, first_name:, last_name:, email:)
        response = json_request(
          method: :post,
          url: SmartProxy::Test::HttpHelpers.join_url(base_url, 'add'),
          body: {
            firstName: first_name,
            lastName: last_name,
            email: email
          }
        )
        raise "Failed to create employee: #{response.code} #{response.body}" unless response.is_a?(Net::HTTPSuccess) || response.code.to_i == 201

        parsed = SmartProxy::Test::HttpHelpers.parse_json(response.body)
        parsed.is_a?(Hash) ? parsed.fetch('id', parsed) : parsed.to_s.delete('"')
      end

      def fetch_employee(base_url, id)
        response = json_request(
          method: :get,
          url: SmartProxy::Test::HttpHelpers.join_url(base_url, id.to_s)
        )

        return nil if response.code.to_i == 404

        raise "Failed to fetch employee: #{response.code} #{response.body}" unless response.is_a?(Net::HTTPSuccess)

        SmartProxy::Test::HttpHelpers.parse_json(response.body)
      end

      def update_employee(base_url, id, first_name:, last_name:, email:)
        response = json_request(
          method: :put,
          url: SmartProxy::Test::HttpHelpers.join_url(base_url, "update/#{id}"),
          body: {
            firstName: first_name,
            lastName: last_name,
            email: email
          }
        )

        raise "Failed to update employee: #{response.code} #{response.body}" unless response.is_a?(Net::HTTPSuccess) || response.code.to_i == 201

        true
      end
    end
  end
end

RSpec.describe 'SmartProxy Server API', :integration do
  include SmartProxy::Test::ServerExamples

  let(:urls) { SmartProxy::Test::Environment.server_urls }
  let(:primary_url) { urls.fetch(:primary) }
  let(:secondary_url) { urls.fetch(:secondary) }

  around do |example|
    availability_check = [primary_url, secondary_url].all? do |base|
      SmartProxy::Test::HttpHelpers.reachable?(SmartProxy::Test::HttpHelpers.join_url(base, 'all'))
    end

    unless availability_check
      skip <<~MESSAGE
        SmartProxy server endpoints were not reachable.
        Ensure at least one server is running and accessible at #{primary_url} or #{secondary_url}.
      MESSAGE
    end

    example.run
  end

  it 'performs a conflict resolution scenario across two servers' do
    employee_id = nil
    begin
      unique_id = SecureRandom.hex(4)
      first_email = "john.#{unique_id}@example.com"
      employee_id = create_employee(primary_url, first_name: 'John', last_name: 'Doe', email: first_email)
      expect(employee_id).not_to be_nil

      sleep SmartProxy::Test::Environment.wait_for_sync_seconds

      primary_employee = fetch_employee(primary_url, employee_id)
      secondary_employee = fetch_employee(secondary_url, employee_id)

      expect(primary_employee).not_to be_nil
      expect(secondary_employee).not_to be_nil

      threads = []
      threads << Thread.new do
        update_employee(primary_url, employee_id, first_name: 'Jane', last_name: 'Smith', email: "jane.#{unique_id}@example.com")
      end
      threads << Thread.new do
        update_employee(secondary_url, employee_id, first_name: 'Jack', last_name: 'Johnson', email: "jack.#{unique_id}@example.com")
      end
      threads.each(&:join)

      sleep SmartProxy::Test::Environment.wait_for_resolution_seconds

      final_primary = fetch_employee(primary_url, employee_id)
      final_secondary = fetch_employee(secondary_url, employee_id)

      expect(final_primary).not_to be_nil
      expect(final_secondary).not_to be_nil

      %w[firstName lastName email].each do |key|
        expect(final_primary[key]).to eq(final_secondary[key])
      end
    ensure
      SmartProxy::Test::HttpHelpers.delete_if_exists(primary_url, employee_id) if employee_id
      SmartProxy::Test::HttpHelpers.delete_if_exists(secondary_url, employee_id) if employee_id
    end
  end
end
