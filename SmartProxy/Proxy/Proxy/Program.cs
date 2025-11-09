using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json",optional: false,reloadOnChange: true);
builder.Services
    .AddOcelot(builder.Configuration)
    .AddCacheManager(x => { x.WithDictionaryHandle(); });

builder.Logging.AddConsole();

var app = builder.Build();
await app.UseOcelot();

app.Run();