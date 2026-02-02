using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PlatformService.AsyncDataServices;
using PlatformService.Data;
using PlatformService.SyncDataServices.Grpc;
using PlatformService.SyncDataServices.Http;
using RabbitMQ.Client;
using System;
using System.IO;

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


Console.WriteLine("--> Using SqlServer Db");
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("PlatformsDB")));


builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
builder.Services.AddHttpClient<ICommandDataClient, HttpCommandDataClient>();
builder.Services.AddSingleton<IMessageBusClient, MessageBusClient>();
builder.Services.AddGrpc();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PlatformService", Version = "v1" });
});

Console.WriteLine($"--> CommandService Endpoint {builder.Configuration["CommandService"]}");

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PlatformService v1"));
}

//app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<GrpcPlatformService>();
app.MapGet("/protos/platforms.proto", async context =>
{
    await context.Response.WriteAsync(File.ReadAllText("Protos/platforms.proto"));
});

PrepDb.PrepPopulation(app, true);

app.Run();
