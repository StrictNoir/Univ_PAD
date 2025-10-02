
using Subscriber.Grpc.CLI;


class Program
{
    static async Task Main(string[] args)
    {
        var cli = new SubscriberCli();
        await cli.RunAsync();
    }
}
