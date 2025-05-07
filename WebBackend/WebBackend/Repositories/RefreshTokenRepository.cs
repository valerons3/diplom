using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext context;
        private readonly ILogger<RefreshTokenRepository> logger;

        public RefreshTokenRepository(AppDbContext context, ILogger<RefreshTokenRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<(bool Success, string? Message)> PostRefreshTokenAsync(RefreshToken token)
        {
            try
            {
                await context.RefreshTokens.AddAsync(token);
                await context.SaveChangesAsync();
                return (true, "Токен успешно сохранен");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при сохранении refresh токена. RefreshToken: {RefreshToken}", token.token);
                return (false, $"Ошибка при сохранении токена: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Message)> ChangeRefreshTokenByUserIdAsync(Guid id, string token)
        {
            try
            {
                var refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == id);
                if (refreshToken == null)
                {
                    return (false, "Токен не найден");
                }

                refreshToken.token = token;
                refreshToken.ExpireDate = DateTime.UtcNow.AddDays(20);

                await context.SaveChangesAsync();
                return (true, "Токен успешно обновлен");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении refresh токена. UserId: {UserId}, RefreshToken: {RefreshToken}",
                    id, token);
                return (false, $"Ошибка при обновлении токена: {ex.Message}");
            }
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            try
            {
                return await context.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.token == token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении refresh токена");
                return null;
            }
        }


        public async Task<(bool Success, string? Message)> DeleteRefreshTokenAsync(RefreshToken token)
        {
            if (token == null)
                return (false, "Токен не может быть null");

            if (token.Id == Guid.Empty)
                return (false, "Некорректный идентификатор токена");

            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                context.RefreshTokens.Remove(token);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Ошибка при удалении refresh токена");
                return (false, $"Неизвестная ошибка: {ex.Message}");
            }
        }
    }
}