using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebBackend.Data;

public class RevokedTokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeSpan interval = TimeSpan.FromHours(6); // Интервал очистки
    private readonly ILogger<RevokedTokenCleanupService> logger;

    public RevokedTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<RevokedTokenCleanupService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    DateTime threshold = DateTime.UtcNow.AddDays(-5);

                    int deletedCount = await dbContext.RevokedTokens
                        .Where(t => t.RevokedAt < threshold)
                        .ExecuteDeleteAsync(stoppingToken);

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при очистке отозванных токенов");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
