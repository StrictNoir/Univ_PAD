namespace Server.Config
{
    public class QueueSettings
    {
        public string Name { get; set; } = string.Empty;
        public bool Durable { get; set; } = true;
        public bool Exclusive { get; set; } = false;
        public bool AutoDelete { get; set; } = false;
    }
}
