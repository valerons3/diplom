using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WebBackend.Configurations;
using WebBackend.Models.DTO;
using WebBackend.Repositories.Interfaces;

public class RabbitConsumerService : BackgroundService
{
    private readonly RabbitmqSettings settings;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly HttpClient httpClient;
    private IConnection connection;
    private IModel channel;

    public RabbitConsumerService(IOptions<RabbitmqSettings> settings, IServiceScopeFactory serviceScopeFactory)
    {
        this.settings = settings.Value;
        this.serviceScopeFactory = serviceScopeFactory;
        httpClient = new HttpClient();
    }

    private void InitializeRabbitMQ()
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.Username,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();

        channel.QueueDeclare(queue: settings.ReceiverQueue,
                              durable: true,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null);
    }

    private void ConsumeMessages()
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                var rabbitData = JsonSerializer.Deserialize<RabbitData>(message);
                if (rabbitData == null) throw new Exception("Ошибка десериализации JSON");

                Console.WriteLine($"Received message: UserID={rabbitData.UserID}, ProcessID={rabbitData.ProcessID}, DownloadLink={rabbitData.DownloadLink}");

                // Обрабатываем файл
                var filePath = await DownloadFileAsync(rabbitData);
                if (filePath != null)
                {
                    await UpdateProcessDataAsync(rabbitData, filePath);
                }

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: settings.ReceiverQueue, autoAck: false, consumer: consumer);
    }

    private async Task<string?> DownloadFileAsync(RabbitData rabbitData)
    {
        var response = await httpClient.GetAsync(rabbitData.DownloadLink);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Ошибка скачивания файла: {response.StatusCode}");
            return null;
        }

        var fileBytes = await response.Content.ReadAsByteArrayAsync();
        var fileName = rabbitData.DownloadLink.Split("fileName=")[^1];
        var resultPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", rabbitData.UserID.ToString(), rabbitData.ProcessID.ToString(), "Result");

        Directory.CreateDirectory(resultPath);
        var filePath = Path.Combine(resultPath, fileName);

        await File.WriteAllBytesAsync(filePath, fileBytes);
        return filePath;
    }

    private async Task UpdateProcessDataAsync(RabbitData rabbitData, string filePath)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dataRepository = scope.ServiceProvider.GetRequiredService<IProcessedDataRepository>();

        var resultChangeData = await dataRepository.ChangeProcessDataAsync(
            rabbitData.Status, filePath, rabbitData.ProcessingTime, rabbitData.ProcessID
        );

        if (!resultChangeData.Sucess)
        {
            Console.WriteLine($"Ошибка обновления данных: {resultChangeData.message}");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeRabbitMQ();
        ConsumeMessages();

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        channel?.Close();
        connection?.Close();
        channel?.Dispose();
        connection?.Dispose();
        httpClient.Dispose();
    }
}
