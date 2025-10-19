namespace Server.Config
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public ExchangeSettings Exchange { get; set; } = new();
        public QueueSettings Queue { get; set; } = new();
    }
}
