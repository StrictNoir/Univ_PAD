

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Subscriber
{
    public class FrameReader
    {
        private readonly NetworkStream _stream;
        private readonly MessageHandler _messageHandler;

        public FrameReader(NetworkStream stream, MessageHandler messageHandler)
        {
            _stream = stream;
            _messageHandler = messageHandler;
        }

        public async Task ReceiveFramesAsync()
        {
            try
            {
                while (true)
                {
                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(_stream, lengthBuffer, lengthBuffer.Length);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }

                    int frameLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
                    if (frameLength <= 0) continue;

                    byte[] payload = new byte[frameLength];
                    read = await ReadExactAsync(_stream, payload, frameLength);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }

                    string json = Encoding.UTF8.GetString(payload);
                    _messageHandler.HandleMessage(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving frames: {ex.Message}");
            }
        }

        private static async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int size)
        {
            int offset = 0;
            while (offset < size)
            {
                int read = await stream.ReadAsync(buffer, offset, size - offset);
                if (read == 0) return 0;
                offset += read;
            }
            return offset;
        }
    }
}
