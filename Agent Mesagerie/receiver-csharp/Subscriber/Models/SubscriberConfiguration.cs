namespace Subscriber.Models
{
    public class SubscriberConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string ConsumerGroup { get; set; } = string.Empty;
        public bool AutoAck { get; set; }
        public List<string> InitialSubjects { get; set; } = new List<string>();
    }
}
