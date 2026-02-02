using Microsoft.Extensions.Configuration;
using PlatformService.Dtos;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public MessageBusClient(IConnection connection)
        {
            _connection = connection;
            try
            {
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout).GetAwaiter().GetResult();

                Console.WriteLine("--> Connected to MessageBus");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        public void PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
        {
            var message = JsonSerializer.Serialize(platformPublishedDto);

            if (_connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
                SendMessage(message);
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connectionids closed, not sending");
            }
        }

        private void SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublishAsync(exchange: "trigger",
                            routingKey: "",
                            body: body).GetAwaiter().GetResult();
            Console.WriteLine($"--> We have sent {message}");
        }

        public void Dispose()
        {
            Console.WriteLine("MessageBus Disposed");
            if (_channel.IsOpen)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _connection.CloseAsync().GetAwaiter().GetResult();
            }
        }
    }
}