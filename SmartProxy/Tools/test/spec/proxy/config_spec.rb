# frozen_string_literal: true

require_relative '../spec_helper'

$LOAD_PATH.unshift(File.expand_path('../../../lib', __dir__))
require 'smart_proxy/tools/proxy_config'

RSpec.describe SmartProxy::Tools::ProxyConfig do
  let(:config_path) { File.expand_path('../../../config/ocelot.json', __dir__) }
  subject(:config) { described_class.load(config_path) }

  it 'exposes each configured route' do
    expect(config.routes.length).to be >= 1
    first_route = config.routes.first

    expect(first_route.upstream_methods).to include('GET')
    expect(first_route.downstream_hosts).to include(include('Host' => 'localhost', 'Port' => 8080))
  end

  it 'indicates that write operations use round robin load balancing' do
    expect(config.round_robin_for?(:put)).to be(true)
    expect(config.round_robin_for?(:post)).to be(true)
    expect(config.round_robin_for?(:delete)).to be(true)
  end

  it 'summarises routes in a Hash format suitable for CLI output' do
    summary = config.summary

    expect(summary).to all(include(:upstream_methods, :downstream_hosts, :round_robin))
    expect(summary.first[:round_robin]).to eq(true)
  end
end
