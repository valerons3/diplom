using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext context;

        public RefreshTokenRepository(AppDbContext context)
        {
            this.context = context;
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
            catch
            {
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
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return (false, $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Неизвестная ошибка: {ex.Message}");
            }
        }
    }
}