using WebBackend.Models.Entity;

namespace WebBackend.Services.Interfaces
{
    public interface IRedisService
    {
        public Task<(bool Success, string? message)> PostUserDataAsync(User user, string token, string code);
        public Task<User?> GetUserDataAsync(string token);
        public Task<bool> CheckEmailCodeAsync(string token, string code);
    }
}
