# frozen_string_literal: true

require 'rspec'
require 'fileutils'
require_relative 'support/environment'
require_relative 'support/http_helpers'

RSpec.configure do |config|
  config.expect_with :rspec do |c|
    c.syntax = :expect
  end

  config.filter_run_when_matching :focus
  persistence_file = File.expand_path('../tmp/examples.txt', __dir__)
  FileUtils.mkdir_p(File.dirname(persistence_file))
  config.example_status_persistence_file_path = persistence_file
  config.disable_monkey_patching!

  config.default_formatter = 'doc' if config.files_to_run.one?

  config.order = :random
  Kernel.srand config.seed
end
