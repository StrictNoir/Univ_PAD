using Subscriber.Models;
using System;
using System.Net;
using System.Net.Sockets;

using System.Text;
using System.Text.Json;


namespace Subscriber
{
    public class SubscriberSocket
    {

        private TcpClient _tcpClient;
        private NetworkStream? _stream;
        private string _address;
        private int _port;
        private FrameWriter? _frameWriter;
        private FrameReader? _frameReader;
        private MessageHandler _messageHandler;

        public bool IsConnected => _tcpClient.Connected;

        public SubscriberSocket(string address, int port)
        {
            _tcpClient = new TcpClient();
            _address = address;
            _port = port;
            _messageHandler = new MessageHandler(_address, _port);
        }

        public async Task ConnectAsync()
        {
            try
            {
                var ipAddress = IPAddress.Parse(_address);
                await _tcpClient.ConnectAsync(ipAddress, _port);
                _stream = _tcpClient.GetStream();

                _frameWriter = new FrameWriter(_stream);
                _frameReader = new FrameReader(_stream, _messageHandler);

                Console.WriteLine("Successfully connected to the broker.");
                _ = Task.Run(() => _frameReader.ReceiveFramesAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to broker: {ex.Message}");
            }
        }

        public async Task SubscribeAsync(string topic)
        {
            if (_frameWriter == null)
            {
                Console.WriteLine("You need to connect to the broker first.");
                return;
            }

            string? from = CheckpointCreator.LoadCheckpoint(_address, _port, topic);

            var frame = new
            {
                op = "SUBSCRIBE",
                topic,
                from
            };

            await _frameWriter.WriteFrameAsync(JsonSerializer.Serialize(frame));
            Console.WriteLine($"Sent SUBSCRIBE for: {topic}");
        }

        public async Task PingAsync()
        {
            if (_frameWriter == null) return;

            var frame = new { op = "PING" };
            await _frameWriter.WriteFrameAsync(JsonSerializer.Serialize(frame));
            Console.WriteLine("Sent PING");
        }

        public void Close()
        {
            _stream?.Close();
            _tcpClient?.Close();
        }
    }
}
