

namespace Subscriber.Models
{
    public class PendingMessage
    {
        public string Subject { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public DateTime ReceivedAt { get; set; }
    }
}
