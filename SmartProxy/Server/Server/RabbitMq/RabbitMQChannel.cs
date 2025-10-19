using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Server.Config;

namespace Server.RabbitMq
{
    public interface IRabbitMQChannel
    {
        IChannel Channel { get; }
        Task InitializeAsync();
    }

    public class RabbitMQChannel : IRabbitMQChannel, IAsyncDisposable
    {
        private IConnection? _connection;
        private readonly IOptions<RabbitMqSettings> _options;
        private readonly ILogger<RabbitMQChannel> _logger;
        public IChannel Channel { get; private set; } = default!;

        public RabbitMQChannel(IOptions<RabbitMqSettings> options, ILogger<RabbitMQChannel> logger)
        {
            _options = options;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            var settings = _options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                UserName = settings.Username,
                Password = settings.Password,
            };

            const int maxRetries = 5;
            const int delaySeconds = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    _logger.LogInformation($"[RabbitMQ] Attempt {attempt}/{maxRetries} to connect...");

                    _connection = await factory.CreateConnectionAsync();
                    Channel = await _connection.CreateChannelAsync();

                    var exchangeName = settings.Exchange.Name;
                    var queueName = settings.Queue.Name;

                    await Channel.ExchangeDeclareAsync(
                        exchange: exchangeName,
                        type: ExchangeType.Fanout,
                        durable: settings.Exchange.Durable,
                        autoDelete: settings.Exchange.AutoDelete);

                    await Channel.QueueDeclareAsync(
                        queue: queueName,
                        durable: settings.Queue.Durable,
                        exclusive: settings.Queue.Exclusive,
                        autoDelete: settings.Queue.AutoDelete);

                    await Channel.QueueBindAsync(queueName, exchangeName, string.Empty);

                    _logger.LogInformation("[RabbitMQ] Connection established successfully.");
                    return;
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning($"[RabbitMQ] Broker unreachable (attempt {attempt}). Message: {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation($"[RabbitMQ] Retrying in {delaySeconds} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    else
                    {
                        _logger.LogError("[RabbitMQ] Max retry attempts reached. Could not connect to broker.");
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[RabbitMQ] Unexpected error while connecting: {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        _logger.LogInformation($"[RabbitMQ] Retrying in {delaySeconds} seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                    }
                    else
                    {
                        _logger.LogError("[RabbitMQ] Max retry attempts reached. Connection failed permanently.");
                        throw;
                    }
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Channel is { IsOpen: true })
                await Channel.CloseAsync();
            if (_connection is { IsOpen: true })
                await _connection.CloseAsync();

            _logger.LogInformation("[RabbitMQ] Connection and channel disposed.");
        }
    }
}
