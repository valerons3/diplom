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

        public RedisService(IConnectionMultiplexer redis)
        {
            database = redis.GetDatabase();
        }

        public async Task DeleteDataAsync(string token)
        {
            await database.KeyDeleteAsync(token);
        }

        public async Task<(bool Success, string? message)> PostUserDataAsync(User user, string token, string code)
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

        public async Task<User?> GetUserDataAsync(string token)
        {
            string? jsonData = await database.StringGetAsync(token);
            if (jsonData == null) return null;

            var storedData = JsonSerializer.Deserialize<StoredUserData>(jsonData);
            return storedData?.User;
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