using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Subscriber.TCP
{
    public class FrameWriter
    {
        private readonly NetworkStream _stream;

        public FrameWriter(NetworkStream stream)
        {
            _stream = stream;
        }

        public async Task WriteFrameAsync(string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));

            await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await _stream.WriteAsync(payload, 0, payload.Length);
            await _stream.FlushAsync();
        }
    }
}
