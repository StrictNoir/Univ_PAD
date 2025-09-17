namespace Subscriber.Models
{
    public class DeliveryMessage
    {
        public string? Op { get; set; }
        public string Topic { get; set; } = string.Empty;
        public MessageContent Message { get; set; } = new MessageContent();
    }
}
