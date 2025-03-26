using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RevokedTokenRepository : IRevokedTokenRepository
    {
        private readonly AppDbContext _context;

        public RevokedTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            try
            {
                return await _context.RevokedTokens
                    .AsNoTracking()
                    .AnyAsync(rt => rt.Token == token);
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        public async Task<(bool Success, string? Message)> PostJWTTokenAsync(string token)
        {
            try
            {
                bool alreadyRevoked = await _context.RevokedTokens
                    .AnyAsync(rt => rt.Token == token);

                if (alreadyRevoked)
                {
                    return (true, "Токен уже был отозван ранее");
                }

                var revokedToken = new RevokedToken
                {
                    Id = Guid.NewGuid(),
                    Token = token,
                    RevokedAt = DateTime.UtcNow  
                };

                await _context.RevokedTokens.AddAsync(revokedToken);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при отзыве токена: {ex.Message}");
            }
        }

    }
}