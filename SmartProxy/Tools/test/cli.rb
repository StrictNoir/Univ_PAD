# frozen_string_literal: true

require 'optparse'

APP_ROOT = File.expand_path(__dir__)
SPEC_ROOT = File.join(APP_ROOT, 'spec')

COMMANDS = {
  'server' => File.join(SPEC_ROOT, 'server'),
  'proxy' => File.join(SPEC_ROOT, 'proxy'),
  'tools' => File.join(SPEC_ROOT, 'tools'),
  'all' => SPEC_ROOT
}.freeze

def ensure_bundle!
  ENV['BUNDLE_GEMFILE'] = File.join(APP_ROOT, 'Gemfile')
  require 'bundler'
rescue LoadError
  warn 'Bundler is required to run the SmartProxy test CLI.'
  warn 'Install it with: gem install bundler'
  exit 1
end

def run_rspec(path, extra_args: [])
  ensure_bundle!

  bundler_cmd = %w[bundle exec rspec]
  bundler_cmd += Array(extra_args)
  bundler_cmd << path

  Dir.chdir(APP_ROOT) do
    system(*bundler_cmd)
  end
end

options = {}

parser = OptionParser.new do |opts|
  opts.banner = <<~BANNER
    SmartProxy Ruby Test CLI
    Usage: ruby cli.rb [command] [options]

    Commands:
      server      Run server integration tests
      proxy       Run proxy configuration tests
      tools       Run Tools specs
      all         Run the entire test suite (default)
  BANNER

  opts.on('--tag TAG', 'Run only examples matching the provided RSpec tag') do |tag|
    options[:tag] = tag
  end

  opts.on('-h', '--help', 'Show this help message') do
    puts opts
    exit
  end
end

parser.order!
command = ARGV.shift || 'all'
path = COMMANDS[command]

unless path
  warn "Unknown command '#{command}'."
  warn parser.help
  exit 1
end

rspec_args = []
rspec_args += ['--tag', options[:tag]] if options[:tag]

exit(run_rspec(path, extra_args: rspec_args) ? 0 : 1)
