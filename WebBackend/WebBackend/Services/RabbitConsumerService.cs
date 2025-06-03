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
    private IConnection connection;
    private IModel channel;
    private readonly ILogger<RabbitConsumerService> logger;

    public RabbitConsumerService(IOptions<RabbitmqSettings> settings, IServiceScopeFactory serviceScopeFactory, ILogger<RabbitConsumerService> logger)
    {
        this.settings = settings.Value;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
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
                if (rabbitData == null)
                {
                    logger.LogError("Ошибка десериализации JSON-сообщения из очереди");
                    return;
                }

                using var scope = serviceScopeFactory.CreateScope();
                var fileService = scope.ServiceProvider.GetRequiredService<IFileService>();
                var dataRepository = scope.ServiceProvider.GetRequiredService<IProcessedDataRepository>();

                if (rabbitData.Status == ProcessStatus.Failed)
                {
                    var resultUpdateData = await dataRepository.ChangeDataIfNotSuccess(rabbitData);
                    if (!resultUpdateData.Success)
                    {
                        return;
                    }
                }
                else
                {
                    var resultDownloadSave = await fileService.DownloadAndSaveResultFilesAsync(rabbitData);

                    if (!resultDownloadSave.Success)
                    {
                        return;
                    }
                    
                    var resultUpdateData = await dataRepository.ChangeDataIfSuccessAsync(rabbitData, 
                        resultDownloadSave.FilePath, resultDownloadSave.ResultImagePath, resultDownloadSave.InputImagePath);
                    if (!resultUpdateData.Success)
                    {
                        return;
                    }
                }
          
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки сообщения из очереди");
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        };

        channel.BasicConsume(queue: settings.ReceiverQueue, autoAck: false, consumer: consumer);
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
    }
}
