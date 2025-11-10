using System.Data;
using Npgsql;
using Server.Config;
using Server.HostedServices;
using Server.MappingProfiles;
using Server.RabbitMq;
using Server.Repositories;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// RabbitMq string section mapping  from appsettings.json
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
// RabbitMQ service registration
builder.Services.AddSingleton(typeof(IRabbitMQService<>), typeof(RabbitMQService<>));
// RabbitMq Channel registration
builder.Services.AddSingleton<IRabbitMQChannel, RabbitMQChannel>(); 
builder.Services.AddSingleton<EmployeeMessageHandler>();
builder.Services.AddHostedService<EmployeeConsumerHostedService>();

// Postgres connection string
var pgConnectionString = builder.Configuration[connection_string];

// Register IDbConnection for Dapper
builder.Services.AddScoped<IDbConnection>(sp =>
{
    var conn = new NpgsqlConnection(pgConnectionString);
    conn.Open();
    return conn;
});

// Repository registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Employee repository registration
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

// Service registration
builder.Services.AddScoped(typeof(IEntityService<,,>), typeof(EntityService<,,>));

// Employee Service registration
builder.Services.AddScoped<IEmployeeService, EmployeeService>();    

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

var channel = app.Services.GetRequiredService<IRabbitMQChannel>();
await channel.InitializeAsync();

app.Run();