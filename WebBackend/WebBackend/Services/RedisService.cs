using System.Text.Json;
using StackExchange.Redis;
using WebBackend.Models.Entity;
using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase database;
        private readonly TimeSpan expiration = TimeSpan.FromMinutes(1); 

        public RedisService(IConnectionMultiplexer redis)
        {
            database = redis.GetDatabase();
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
                : (false, "Ошибка при сохранении данных в Redis.");
        }

        public async Task<User?> GetUserDataAsync(string token)
        {
            string? jsonData = await database.StringGetAsync(token);
            if (jsonData == null) return null;

            var storedData = JsonSerializer.Deserialize<StoredUserData>(jsonData);
            return storedData?.User;
        }

        public async Task<bool> CheckEmailCodeAsync(string token, string code)
        {
            string? jsonData = await database.StringGetAsync(token);
            if (jsonData == null) return false;

            var storedData = JsonSerializer.Deserialize<StoredUserData>(jsonData);
            return storedData?.Code == code;
        }
    }

    internal class StoredUserData
    {
        public User User { get; set; } = default!;
        public string Code { get; set; } = string.Empty;
    }
}
