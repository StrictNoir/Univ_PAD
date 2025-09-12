using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Subscriber
{
    public class SubscriberSocket
    {
        private TcpClient _tcpClient;
        private NetworkStream? _stream;
        public bool IsConnected => _tcpClient.Connected;

        public SubscriberSocket()
        {
            _tcpClient = new TcpClient();
        }

        public async Task ConnectAsync(IPAddress address, int port)
        {
            try
            {
                await _tcpClient.ConnectAsync(address, port);
                _stream = _tcpClient.GetStream();
                Console.WriteLine("Successfully connected to the broker.");

           
                _ = Task.Run(ReceiveFramesAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to broker: {ex.Message}");
            }
        }

        public async Task SubscribeAsync(string topic, string from)
        {
            if (_stream == null)
            {
                Console.WriteLine("You need to connect to the broker first.");
                return;
            }

            var frame = new
            {
                op = "SUBSCRIBE",
                topic,
                from
            };

            await WriteFrameAsync(JsonSerializer.Serialize(frame));
            Console.WriteLine($"Sent SUBSCRIBE for: {topic}");
        }

        public async Task PingAsync()
        {
            if (_stream == null) return;
            var frame = new { op = "PING" };
            await WriteFrameAsync(JsonSerializer.Serialize(frame));
            Console.WriteLine("Sent PING");
        }

        private async Task WriteFrameAsync(string message)
        {
            if (_stream == null) return;

            byte[] payload = Encoding.UTF8.GetBytes(message);
            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));

            await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
            await _stream.WriteAsync(payload, 0, payload.Length);
            await _stream.FlushAsync();
        }

        private async Task ReceiveFramesAsync()
        {
            if (_stream == null) return;

            try
            {
                while (true)
                {
                    
                    byte[] lengthBuffer = new byte[4];
                    // citeste mai intai lungimea mesajului care va veni
                    int read = await ReadExactAsync(_stream, lengthBuffer, lengthBuffer.Length);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }
                    // numarul de byti necesari pentru mesaj
                    int frameLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
                    if (frameLength <= 0) continue;

                    
                    byte[] payload = new byte[frameLength];
                    // citeste payloadul
                    read = await ReadExactAsync(_stream, payload, frameLength);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }

                    string message = Encoding.UTF8.GetString(payload);
                    Console.WriteLine($"Received: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while receiving frames: {ex.Message}");
            }
            finally
            {
                Close();
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

        public void Close()
        {
            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); } catch { }
        }
    }
}
