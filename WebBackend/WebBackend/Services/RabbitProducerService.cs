using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using WebBackend.Configurations;
using WebBackend.Models.DTO;
using WebBackend.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using System.Text.Unicode;

public class RabbitProducerService : IRabbitProducerService
{
    private readonly RabbitmqSettings settings;

    public RabbitProducerService(IOptions<RabbitmqSettings> settings)
    {
        this.settings = settings.Value;
    }

    public (bool Success, string? Message) Publish(RabbitData data)
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: settings.SenderQueue,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);


            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
            };
            var message = JsonSerializer.Serialize(data, options);  
            var body = Encoding.UTF8.GetBytes(message);
            Console.WriteLine($"Message: {message}");
            Console.WriteLine($"Body: {Encoding.UTF8.GetString(body)}");

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: settings.SenderQueue,
                                 basicProperties: properties,
                                 body: body);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Ошибка при публикации данных в очередь");
        }
    }
}
