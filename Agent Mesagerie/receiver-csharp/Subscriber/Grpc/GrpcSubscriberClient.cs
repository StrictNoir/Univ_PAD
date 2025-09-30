
using Grpc.Net.Client;

namespace Subscriber.Grpc
{
    public class GrpcSubscriberClient
    {
        private readonly string _address;
        private readonly int _port;
        private GrpcChannel? _channel;
        private Broker.BrokerClient? _client;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly MessageHandler _messageHandler;
        private readonly string _consumerGroup;
        private readonly bool _autoAck;

        public GrpcSubscriberClient(string address, int port, string consumerGroup = "", bool autoAck = false)
        {
            _address = address;
            _port = port;
            _consumerGroup = consumerGroup;
            _autoAck = autoAck;
            _messageHandler = new MessageHandler();
            _subscriptionManager = new SubscriptionManager();
        }
        public void Connect()
        {
            try
            {
                var endpoint = $"http://{_address}:{_port}";
                _channel = GrpcChannel.ForAddress(endpoint);
                _client = new Broker.BrokerClient(_channel);
                Console.WriteLine($"Connected to broker at {endpoint}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to broker: {ex.Message}");
                throw;
            }
        }
        public async Task SubscribeAsync(string subject)
        {
            if (_client == null)
            {
                Console.WriteLine("You need to connect to the broker first.");
                await Task.CompletedTask;
            }

            subject = subject.Trim();
            if (string.IsNullOrEmpty(subject))
            {
                Console.WriteLine("Subject cannot be empty.");
                await Task.CompletedTask;
            }

            if (_subscriptionManager.HasSubscription(subject))
            {
                Console.WriteLine($"Already subscribed to {subject}");
                await Task.CompletedTask;
            }

            var subscription = new Subscription
            {
                Subject = subject,
                ConsumerGroup = _consumerGroup
            };

            try
            {
                var entry = _subscriptionManager.CreateSubscription(subject);
                var call = _client?.Subscribe(subscription, cancellationToken: entry.CancellationTokenSource.Token);
                entry.Call = call;

                // Start receiving messages in a background task
                var receiver = new MessageReceiver(_messageHandler, _autoAck);
                entry.Task = Task.Run(async () => await receiver.ReceiveMessagesAsync(subject, call!, entry.CancellationTokenSource.Token));

                Console.WriteLine($"Subscribed to \"{subject}\"");
            }
            catch (Exception ex)
            {
                _subscriptionManager.RemoveSubscription(subject);
                Console.WriteLine($"Failed to start subscription for \"{subject}\": {ex.Message}");
            }
        }

        public void Unsubscribe(string subject)
        {
            if (_subscriptionManager.RemoveSubscription(subject, out var entry))
            {
                entry!.CancellationTokenSource.Cancel();

                try
                {
                    entry.Task?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // Expected when task is cancelled
                }

                entry.CancellationTokenSource.Dispose();
                Console.WriteLine($"Unsubscribed from \"{subject}\"");
            }
            else
            {
                Console.WriteLine($"Not subscribed to \"{subject}\"");
            }
        }
        public void UnsubscribeAll()
        {
            var subjects = _subscriptionManager.GetAllSubscriptions();
            foreach (var subject in subjects)
            {
                Unsubscribe(subject);
            }
        }
        public void ListSubscriptions()
        {
            var subjects = _subscriptionManager.GetAllSubscriptions();
            if (subjects.Count == 0)
            {
                Console.WriteLine("No active subscriptions.");
            }
            else
            {
                Console.WriteLine($"Active subscriptions: {string.Join(", ", subjects)}");
            }
        }
        public async Task Shutdown()
        {
            Console.WriteLine("Disconnecting...");
            UnsubscribeAll();

            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel.Dispose();
            }

            Console.WriteLine("Disconnected.");
        }
    }
}
