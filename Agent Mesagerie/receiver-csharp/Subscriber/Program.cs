using Subscriber;
using Subscriber.Grpc.CLI;


class Program
{
    static async Task Main(string[] args)
    {
        var cli = new SubscriberCli(args);
        await cli.RunAsync();
    }
}
