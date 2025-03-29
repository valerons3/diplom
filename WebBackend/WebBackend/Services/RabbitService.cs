using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WebBackend.Configurations;
using WebBackend.Models.DTO;
using WebBackend.Services.Interfaces;

public class RabbitService : IRabbitService
{
    private readonly RabbitmqSettings settings;

    public RabbitService(IOptions<RabbitmqSettings> settings)
    {
        this.settings = settings.Value;
    }

    public async Task<(bool Success, string? Message)> PublishAsync(RabbitData data)
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

            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: settings.SenderQueue,
                                            durable: true,
                                            exclusive: false,
                                            autoDelete: false,
                                            arguments: null);

            var message = JsonSerializer.Serialize(data);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true
            };
            properties.Persistent = true;

            await channel.BasicPublishAsync(exchange: string.Empty,
                                            routingKey: settings.SenderQueue,
                                            mandatory: false,
                                            basicProperties: properties,
                                            body: body);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, "Ошибка при публикации данных в очередь");
        }
    }
}
