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
using WebBackend.Models.DTO;
using WebBackend.Models.Enums;
using WebBackend.Configurations;
using WebBackend.Repositories.Interfaces;
using WebBackend.Services.Interfaces;

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

                if (rabbitData.Status == ProcessStatus.Failed)
                {
                    await UpdateProcessDataAsync(rabbitData, null, null);
                }
                else
                {
                    var resultDownload = await DownloadFilesAsync(rabbitData);
                    if (resultDownload.filePath != null && resultDownload.imagePath != null)
                    {
                        await UpdateProcessDataAsync(rabbitData, resultDownload.filePath, resultDownload.imagePath);
                    }
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

    private async Task<(string? filePath, string? imagePath)> DownloadFilesAsync(RabbitData rabbitData)
    {
        var responseFile = await httpClient.GetAsync(rabbitData.DownloadLink);
        var responseImage = await httpClient.GetAsync(rabbitData.ImageDownloadLink);
        if (!responseFile.IsSuccessStatusCode || !responseImage.IsSuccessStatusCode)
        {
            Console.WriteLine($"Ошибка скачивания файла: {responseFile.StatusCode}");
            return (null, null);
        }

        var imageBytes = await responseImage.Content.ReadAsByteArrayAsync();
        var imageName = rabbitData.ImageDownloadLink.Split("fileName=")[^1];
        var fileBytes = await responseFile.Content.ReadAsByteArrayAsync();
        var fileName = rabbitData.DownloadLink.Split("fileName=")[^1];

        using var scope = serviceScopeFactory.CreateScope();
        var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();

        var imageSaveResult = await fileService.SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID, 
            imageBytes, imageName);
        var fileSaveResult = await fileService.SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID,
            fileBytes, fileName);

        return (fileSaveResult.Message, imageSaveResult.Message);
    }

    private async Task UpdateProcessDataAsync(RabbitData rabbitData, string? filePath, string? imagePath)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dataRepository = scope.ServiceProvider.GetRequiredService<IProcessedDataRepository>();

        var resultChangeData = await dataRepository.ChangeProcessDataAsync(
            rabbitData.Status, filePath, imagePath, rabbitData.ProcessingTime, rabbitData.ProcessID
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
