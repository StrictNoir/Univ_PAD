using RabbitMQ.Client;

namespace Server.Config
{
    public class ExchangeSettings
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = ExchangeType.Fanout;
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
    }
}
