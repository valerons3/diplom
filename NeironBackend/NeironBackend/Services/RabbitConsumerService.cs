using Microsoft.Extensions.Options;
using NeironBackend.Configurations;
using NeironBackend.Models;
using NeironBackend.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NeironBackend.Services
{
    public class RabbitConsumerService : BackgroundService
    {
        private readonly RabbitmqSettings settings;
        private readonly HttpClient httpClient;
        private readonly DownloadURL settingsURL;
        private IConnection connection;
        private IModel channel;
        private IRabbitProducer producer;

        public RabbitConsumerService(IOptions<RabbitmqSettings> options, IOptions<DownloadURL> downloadURL, IRabbitProducer producer)
        {
            settings = options.Value;
            settingsURL = downloadURL.Value;
            httpClient = new HttpClient();
            this.producer = producer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                await ProcessMessageAsync(message, ea.DeliveryTag);
            };

            channel.BasicConsume(queue: settings.ReceiverQueue, autoAck: false, consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessMessageAsync(string message, ulong deliveryTag)
        {
            try
            {
                var rabbitData = JsonSerializer.Deserialize<RabbitData>(message);
                if (rabbitData == null) throw new Exception("Ошибка десериализации JSON");

                Console.WriteLine($"[→] Получено сообщение: UserID={rabbitData.UserID}, ProcessID={rabbitData.ProcessID}");

                var response = await httpClient.GetAsync(rabbitData.DownloadLink);
                if (!response.IsSuccessStatusCode) throw new Exception($"Ошибка загрузки файла: {response.StatusCode}");

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = rabbitData.DownloadLink.Split("fileName=")[^1];
                var basePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", rabbitData.UserID.ToString(), rabbitData.ProcessID.ToString());

                var inputPath = Path.Combine(basePath, "Input");
                var resultPath = Path.Combine(basePath, "Result");

                Directory.CreateDirectory(inputPath);
                Directory.CreateDirectory(resultPath);

                var inputFilePath = Path.Combine(inputPath, fileName);
                var resultFilePath = Path.Combine(resultPath, fileName);

                await File.WriteAllBytesAsync(inputFilePath, fileBytes);
                Console.WriteLine($"[✔] Файл скачан: {inputFilePath}");

                // Имитация работы нейросети
                Console.WriteLine($"[⏳] Имитация обработки файла...");
                await Task.Delay(TimeSpan.FromSeconds(5));

                // Копируем файл в папку Result (здесь должна быть реальная обработка)
                File.Copy(inputFilePath, resultFilePath, true);
                Console.WriteLine($"[✔] Обработка завершена, файл сохранен в: {resultFilePath}");

                var link = $"{settingsURL.BaseUrl}userID={rabbitData.UserID}&processID={rabbitData.ProcessID}&fileName={fileName}";
                Console.WriteLine(link);
                RabbitData rabbitData1 = new RabbitData()
                {
                    UserID = rabbitData.UserID,
                    ProcessID = rabbitData.ProcessID,
                    ProcessingTime = TimeSpan.FromSeconds(5),
                    DownloadLink = $"{settingsURL.BaseUrl}userID={rabbitData.UserID}&processID={rabbitData.ProcessID}&fileName={fileName}"
                };
                var resultPublish = producer.Publish(rabbitData1);

                channel.BasicAck(deliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Ошибка обработки сообщения: {ex.Message}");
            }
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
}
