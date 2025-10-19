
using DataLayer.Entities;

namespace Server.RabbitMq
{
    public interface IRabbitMQService<T> where T : Document
    {
        Task PublishMessageAsync(Message<T> message);
        Task StartConsumer(Func<Message<T>, Task> handler);
    }
}
