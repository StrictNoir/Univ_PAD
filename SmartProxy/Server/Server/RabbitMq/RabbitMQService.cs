using DataLayer.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server.Config;
using System.Text;
using System.Text.Json;

namespace Server.RabbitMq
{
    public class RabbitMQService<T>: IRabbitMQService<T> where T : Document
    {
        private readonly IRabbitMQChannel _rabbitChannel;
        private readonly ILogger<RabbitMQService<T>> _logger;
        private readonly RabbitMqSettings _settings;
        private IChannel _channel;
        
        public RabbitMQService(IOptions<RabbitMqSettings> options, 
            ILogger<RabbitMQService<T>> logger, IRabbitMQChannel channel)
        {
            _logger = logger;
            _rabbitChannel = channel;
            _settings = options.Value;
            _channel = _rabbitChannel.Channel;
        }

        public async Task PublishMessageAsync(Message<T> message)
        {
            try
            {
                var exchangeName = _settings.ExchangeSettings.Name;

                var json = JsonSerializer.Serialize(message);

                var body = Encoding.UTF8.GetBytes(json);

                await _channel.BasicPublishAsync(exchange: exchangeName, "",body);
                _logger.LogInformation($"Published message to {exchangeName}: {json}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message.");
                throw;
            }
           
        }
        public async Task StartConsumer(Func<Message<T>,Task> handler)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = JsonSerializer.Deserialize<Message<T>>(body);

                    if (message == null) return;

                    await handler(message);

                    await _channel.BasicAckAsync(ea.DeliveryTag, false);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message.");

                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };
           await _channel.BasicConsumeAsync(queue: _settings.QueueSettings.Name,autoAck:false,consumer: consumer);

        }
    }
}
