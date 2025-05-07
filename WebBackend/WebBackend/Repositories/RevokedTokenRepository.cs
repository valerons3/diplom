using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RevokedTokenRepository : IRevokedTokenRepository
    {
        private readonly AppDbContext context;
        private readonly ILogger<RevokedTokenRepository> logger;

        public RevokedTokenRepository(AppDbContext context, ILogger<RevokedTokenRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<bool> IsTokenRevokedAsync(string token)
        {
            try
            {
                return await context.RevokedTokens
                    .AsNoTracking()
                    .AnyAsync(rt => rt.Token == token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке отозванного JWT токена. JWTToken: {JWTToken}", token);
                return true;
            }
        }

        public async Task<(bool Success, string? Message)> PostJWTTokenAsync(string token)
        {
            try
            {
                bool alreadyRevoked = await context.RevokedTokens
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

                await context.RevokedTokens.AddAsync(revokedToken);
                await context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при отзыве JWT токена. JWTToken: {JWTToken}", token);
                return (false, $"Ошибка при отзыве токена: {ex.Message}");
            }
        }

    }
}