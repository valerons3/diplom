using System.Text.Json;
using StackExchange.Redis;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Models.Enums;
using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase database;
        private readonly TimeSpan expiration = TimeSpan.FromMinutes(3);
        private readonly ILogger<RedisService> logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            database = redis.GetDatabase();
            this.logger = logger;
        }

        public async Task DeleteDataAsync(string token)
        {
            try
            {
                await database.KeyDeleteAsync(token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при удалении данных из Redis. Token: {Token}", token);
            }
        }

        public async Task<(bool Success, string? message)> PostUserDataAsync(User user, string token, string code)
        {
            try
            {
                var storedData = new StoredUserData
                {
                    User = user,
                    Code = code
                };

                string jsonData = JsonSerializer.Serialize(storedData);
                bool isSet = await database.StringSetAsync(token, jsonData, expiration);

                return isSet
                    ? (true, null)
                    : (false, "Ошибка при сохранении данных в Redis");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при сохранении данных в Redis. Token: {Token}", token);
                return (false, "Внутренняя ошибка при работе с кэшем");
            }
        }

        public async Task<User?> GetUserDataAsync(string token)
        {
            try
            {
                string? jsonData = await database.StringGetAsync(token);
                if (jsonData == null) return null;

                var storedData = JsonSerializer.Deserialize<StoredUserData>(jsonData);
                return storedData?.User;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении данных пользователя из Redis. Token: {Token}", token);
                return null;
            }
        }

        public async Task<EmailVerificationStatus> CheckEmailCodeAsync(string token, string code)
        {
            string? jsonData = await database.StringGetAsync(token);

            if (jsonData == null)
            {
                return EmailVerificationStatus.NotFound;
            }

            var storedData = JsonSerializer.Deserialize<StoredUserData>(jsonData);

            if (storedData?.Code == code)
            {
                return EmailVerificationStatus.CodeValid;
            }

            return EmailVerificationStatus.CodeInvalid;
        }
    }

}