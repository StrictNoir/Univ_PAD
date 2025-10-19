
using DataLayer.Dtos;
using DataLayer.Entities;

namespace Server.RabbitMq
{
    public class Message<T> : Document
    {
        public MessageType MessageType {  get; set; }
        public T? Payload { get; set; }

    }
}
