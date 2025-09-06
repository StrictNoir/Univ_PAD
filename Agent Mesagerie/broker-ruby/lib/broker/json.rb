# lib/broker/json.rb
# frozen_string_literal: true

module Broker
  module Json
    begin
      require 'oj'
      LOAD = ->(s) { Oj.load(s, mode: :strict) }
      DUMP = ->(o) { Oj.dump(o, mode: :strict) }
    rescue LoadError
      require 'json'
      LOAD = ->(s) { JSON.parse(s) }
      DUMP = ->(o) { JSON.generate(o) }
    end
  end
end
