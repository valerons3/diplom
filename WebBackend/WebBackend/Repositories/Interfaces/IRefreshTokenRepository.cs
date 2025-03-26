using WebBackend.Models.Entity;

namespace WebBackend.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        public Task<(bool Success, string? Message)> PostRefreshTokenAsync(RefreshToken token);
        public Task<(bool Success, string? Message)> DeleteRefreshTokenAsync(RefreshToken token);
        public Task<(bool Success, string? Message)> ChangeRefreshTokenByUserIdAsync(Guid id, string token);
        public Task<RefreshToken?> GetRefreshTokenAsync(string token);
    }
}