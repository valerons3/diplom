using WebBackend.Models.Entity;

namespace WebBackend.Services.Interfaces
{
    public enum EmailVerificationStatus
    {
        CodeValid,
        CodeInvalid,
        NotFound
    }
    public interface IRedisService
    {
        public Task<(bool Success, string? message)> PostUserDataAsync(User user, string token, string code);
        public Task<User?> GetUserDataAsync(string token);
        public Task<EmailVerificationStatus> CheckEmailCodeAsync(string token, string code);
        public Task DeleteDataAsync(string token);
    }
}