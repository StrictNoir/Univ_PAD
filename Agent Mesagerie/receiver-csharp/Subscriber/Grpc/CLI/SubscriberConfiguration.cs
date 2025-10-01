
namespace Subscriber.Grpc.CLI
{
    public class SubscriberConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool AutoAck { get; set; }
        public List<string> InitialSubjects { get; set; } = new List<string>();
    }
}
