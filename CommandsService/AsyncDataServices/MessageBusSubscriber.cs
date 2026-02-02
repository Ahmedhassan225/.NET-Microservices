using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandsService.EventProcessing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandsService.AsyncDataServices
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IEventProcessor _eventProcessor;
        private readonly IConnection _connection;
        private IChannel _channel;
        private string _queueName;

        public MessageBusSubscriber(
            IConnection connection,
            IEventProcessor eventProcessor)
        {
            _connection = connection;
            _eventProcessor = eventProcessor;

            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout).GetAwaiter().GetResult();
                var queueDeclareResult = _channel.QueueDeclareAsync().GetAwaiter().GetResult();
                _queueName = queueDeclareResult.QueueName;
                _channel.QueueBindAsync(queue: _queueName,
                    exchange: "trigger",
                    routingKey: "").GetAwaiter().GetResult();

                Console.WriteLine("--> Listenting on the Message Bus...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            if (_channel == null)
            {
                Console.WriteLine("--> No channel available, Message Bus subscriber not active");
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (sender, ea) =>
            {
                Console.WriteLine("--> Event Received!");

                var body = ea.Body;
                var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

                _eventProcessor.ProcessEvent(notificationMessage);
                await Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer);
        }

        public override void Dispose()
        {
            if(_channel != null && _channel.IsOpen)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _connection.CloseAsync().GetAwaiter().GetResult();
            }

            base.Dispose();
        }
    }
}