# lib/broker/framing.rb
# frozen_string_literal: true

module Broker
  module Framing
    MAX = (2**32) - 1

    module_function

    def read_exact(io, n)
      buf = +''
      while buf.bytesize < n
        chunk = io.readpartial(n - buf.bytesize)
        buf << chunk
      end
      buf
    rescue EOFError, IOError, SystemCallError
      nil
    end

    def read_frame(io, max: MAX)
      len_b = read_exact(io, 4) or return nil
      len = len_b.unpack1('N')
      return nil if len <= 0 || len > max

      read_exact(io, len)
    end

    def write_frame(io, payload)
      io.write([payload.bytesize].pack('N'))
      io.write(payload)
      io.flush
      true
    rescue IOError, SystemCallError
      false
    end
  end
end
