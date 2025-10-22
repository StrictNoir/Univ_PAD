# frozen_string_literal: true

require_relative '../spec_helper'
require 'stringio'

$LOAD_PATH.unshift(File.expand_path('../../../lib', __dir__))
require 'smart_proxy/tools/http_client_demo'
require 'webmock/rspec'

RSpec.describe SmartProxy::Tools::HttpClientDemo do
  include WebMock::API

  let(:logger) { StringIO.new }
  let(:sleeper) { ->(_seconds) {} }
  let(:base_attributes) do
    {
      'FirstName' => 'John',
      'LastName' => 'Doe',
      'Email' => 'john.doe@example.com',
      'LastChangedAt' => '2024-01-01T00:00:00Z'
    }
  end

  before do
    WebMock.enable!
    WebMock.disable_net_connect!(allow_localhost: true)
  end

  after do
    WebMock.allow_net_connect!
    WebMock.disable!
  end

  it 'creates an employee and parses the returned identifier' do
    demo = described_class.new(
      server1_url: 'http://server1/api/Employee',
      server2_url: 'http://server2/api/Employee',
      sleeper: sleeper,
      logger: logger
    )

    stub_request(:post, 'http://server1/api/Employee/add')
      .with(body: hash_including(FirstName: 'John'))
      .to_return(status: 200, body: '"abc-123"')

    id = demo.create_employee('http://server1/api/Employee', 'John', 'Doe', 'john@example.com')

    expect(id).to eq('abc-123')
  end

  it 'runs the full conflict scenario and returns a consistent result summary' do
    demo = described_class.new(
      server1_url: 'http://server1/api/Employee',
      server2_url: 'http://server2/api/Employee',
      sleeper: sleeper,
      logger: logger
    )

    stub_request(:post, 'http://server1/api/Employee/add')
      .to_return(status: 200, body: '"employee-42"')

    final_payload = base_attributes.merge(
      'FirstName' => 'Jane',
      'LastName' => 'Smith',
      'Email' => 'jane.smith@example.com',
      'LastChangedAt' => '2024-01-01T00:00:05Z'
    )

    stub_request(:get, 'http://server1/api/Employee/employee-42')
      .to_return(
        { status: 200, body: base_attributes.merge('LastChangedAt' => '2024-01-01T00:00:00Z').to_json },
        { status: 200, body: final_payload.to_json }
      )

    stub_request(:get, 'http://server2/api/Employee/employee-42')
      .to_return(
        { status: 200, body: base_attributes.merge('LastChangedAt' => '2024-01-01T00:00:01Z').to_json },
        { status: 200, body: final_payload.to_json }
      )

    stub_request(:put, 'http://server1/api/Employee/update/employee-42')
      .to_return(status: 200)

    stub_request(:put, 'http://server2/api/Employee/update/employee-42')
      .to_return(status: 200)

    result = demo.perform_conflict_test

    expect(result.employee_id).to eq('employee-42')
    expect(result).to be_consistent
    expect(result.server1['FirstName']).to eq('Jane')
  end
end
