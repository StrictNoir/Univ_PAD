using Subscriber.Models;
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

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
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
                    // 1. Read the 4-byte length prefix
                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(_stream, lengthBuffer, lengthBuffer.Length);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }

                    int frameLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBuffer, 0));
                    if (frameLength <= 0) continue;

                    // 2. Read the JSON payload
                    byte[] payload = new byte[frameLength];
                    read = await ReadExactAsync(_stream, payload, frameLength);
                    if (read == 0)
                    {
                        Console.WriteLine("Connection closed by broker.");
                        break;
                    }

                    string json = Encoding.UTF8.GetString(payload);
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        string? op = doc.RootElement.GetProperty("op").GetString();

                        switch (op)
                        {
                            case "DELIVER":
                                HandleDeliveryMessage(json);
                                break;
                            case "SUBSCRIBED":
                                HandleSubscribedMessage(json);
                                break;
                            case "PONG":
                                Console.WriteLine("Received PONG response from broker");
                                break;
                            default:
                                Console.WriteLine($"Received unknown op: {op}");
                                Console.WriteLine($"Raw JSON: {json}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse frame: {ex.Message}");
                    }
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

        private void HandleDeliveryMessage(string json)
        {
            var deliver = JsonSerializer.Deserialize<DeliveryMessage>(json, _jsonOptions);
            if (deliver != null)
            {
                Console.WriteLine("=== New DELIVER Message ===");
                Console.WriteLine($"Topic: {deliver.Topic}");
                Console.WriteLine($"Title: {deliver.Message.Title}");
                Console.WriteLine($"Content: {deliver.Message.Content}");
                Console.WriteLine("===========================");
            }
        }
        private void HandleSubscribedMessage(string json)
        {
            var subscribed = JsonSerializer.Deserialize<SubscribedMessage>(json, _jsonOptions);
            if (subscribed != null)
            {
                Console.WriteLine($"Successfully subscribed to topic: {subscribed.Topic}");
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
