using Subscriber;
using Subscriber.Grpc.CLI;


class Program
{
    static async Task Main(string[] args)
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var cli = new SubscriberCli(args);
        await cli.RunAsync();
    }
}
