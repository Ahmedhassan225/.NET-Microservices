using CommandsService.AsyncDataServices;
using CommandsService.Data;
using CommandsService.EventProcessing;
using CommandsService.SyncDataServices.Grpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

if (builder.Configuration.GetConnectionString("rabbitmq") != null)
{
    builder.AddRabbitMQClient("rabbitmq");
}
else
{
    builder.Services.AddSingleton<IConnection>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var factory = new ConnectionFactory()
        {
            HostName = configuration["RabbitMQHost"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQPort"] ?? "5672")
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    });
}

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMen"));
builder.Services.AddScoped<ICommandRepo, CommandRepo>();
builder.Services.AddControllers();

builder.Services.AddHostedService<MessageBusSubscriber>();

builder.Services.AddSingleton<IEventProcessor, EventProcessor>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddScoped<IPlatformDataClient, PlatformDataClient>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CommandsService", Version = "v1" });
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CommandsService v1"));
}

//app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

PrepDb.PrepPopulation(app);

app.Run();
