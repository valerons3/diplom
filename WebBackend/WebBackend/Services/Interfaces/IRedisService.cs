using WebBackend.Models.Entity;
using WebBackend.Models.Enums;

namespace WebBackend.Services.Interfaces
{
    public interface IRedisService
    {
        public Task<(bool Success, string? message)> PostUserDataAsync(User user, string token, string code);
        public Task<User?> GetUserDataAsync(string token);
        public Task<EmailVerificationStatus> CheckEmailCodeAsync(string token, string code);
        public Task DeleteDataAsync(string token);
    }
}