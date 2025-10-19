using Microsoft.Extensions.Options;
using RabbitMQ.Client;
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
        public IChannel Channel { get; private set; } = default!;

        public RabbitMQChannel(IOptions<RabbitMqSettings> options)
        {
            _options = options;
        }

        public async Task InitializeAsync()
        {
            var settings = _options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                UserName = settings.Username,
                Password = settings.Password
            };

            _connection = await factory.CreateConnectionAsync();
            Channel = await _connection.CreateChannelAsync();

  
            var exchangeName = settings.ExchangeSettings.Name;
            var queueName = settings.QueueSettings.Name;

            await Channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Fanout,
                durable: settings.ExchangeSettings.Durable, autoDelete: settings.ExchangeSettings.AutoDelete);

            await Channel.QueueDeclareAsync(queueName,
                durable: settings.QueueSettings.Durable,
                exclusive: settings.QueueSettings.Exclusive,
                autoDelete: settings.QueueSettings.AutoDelete);

            await Channel.QueueBindAsync(queueName, exchangeName, "");
        }

        public async ValueTask DisposeAsync()
        {
            if (Channel is { IsOpen: true })
                await Channel.CloseAsync();
            if (_connection is { IsOpen: true })
                await _connection.CloseAsync();
        }


    }
}
