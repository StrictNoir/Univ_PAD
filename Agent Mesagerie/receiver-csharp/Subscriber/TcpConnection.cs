
using System.Net;
using System.Net.Sockets;

namespace Subscriber
{
    public class TcpConnection
    {
        private TcpClient _tcpClient;
        private NetworkStream? _stream;

        public bool IsConnected => _tcpClient.Connected;
        public string Address { get; }
        public int Port { get; }

        public TcpConnection(string address, int port)
        {
            _tcpClient = new TcpClient();
            Address = address;
            Port = port;
        }

        public async Task ConnectAsync()
        {
            await _tcpClient.ConnectAsync(IPAddress.Parse(Address), Port);
            _stream = _tcpClient.GetStream();
        }

        public async Task SendAsync(byte[] data)
        {
            if (_stream == null) return;
            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            await _stream.WriteAsync(lengthPrefix);
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        public async Task<int> ReadExactAsync(byte[] buffer, int size)
        {
            int offset = 0;
            while (offset < size)
            {
                int read = await _stream!.ReadAsync(buffer, offset, size - offset);
                if (read == 0) return 0;
                offset += read;
            }
            return offset;
        }

        public void Close()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
    }
}
