
using MongoDB.Driver;
using Server.MappingProfiles;
using Server.Repositories;
using Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Connection string section extraction from Appsettings.json
var mongoHost = builder.Configuration["MONGO_HOST"];
var mongoDatabaseName = builder.Configuration["MONGO_DB_NAME"];
var mongoConnectionString = $"mongodb://{mongoHost}:27017";

// MongoClient driver setup

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    return new MongoClient(mongoConnectionString);
});
// Getting database
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
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

app.Run();
