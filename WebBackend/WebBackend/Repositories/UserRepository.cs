using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebBackend.Data;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext context;
        private readonly ILogger<UserRepository> logger;
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<(bool Success, string? message)> CheckUserExistsAsync(string email)
        {
            try
            {
                var existingUser = await context.Users.AnyAsync(u => u.Email == email);
                if (existingUser)
                {
                    return (true, "Пользователь с таким Email уже существует");
                }
                else
                {
                    return (false, null);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке существования пользователя. Email: {Email}", email);
                return (false, null);
            }
        }

        public async Task<Register?> GetUserInfoByIdAsync(Guid id)
        {
            try
            {
                var user = await context.Users.FindAsync(id);
                if (user == null)
                {
                    return null;
                }
                var role = context.Roles.FirstOrDefault(r => r.Id == id);
                if (role == null)
                {
                    return null;
                }

                return new Register
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Password = user.PasswordHash,
                    Role = role.Name
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении информации о пользователе. UserId: {UserId}", id);
                return null;
            }
        }

        public async Task<(bool Success, string? message)> PostUserAsync(User user)
        {
            try
            {
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
                return (true, "Регистрация прошла успешно");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при регистрации пользователя");
                return (false, "Ошибка при регистрации пользователя");
            }
        }

        public async Task<User?> GetEntityUserByEmailAsync(string email)
        {
            User? user;
            try
            {
                user = await context.Users.Include(u => u.UserRefreshToken).FirstOrDefaultAsync(u => u.Email == email);
                return user;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении Entity сущности пользователя по email. Email: {Email}", email);
                return null;
            }
        }

        public async Task<UserDTO> GetUserByEmailAsync(string email)
        {
            try
            {
                User? user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null) return null;

                UserDTO userDto = new()
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                };
                
                return userDto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении DTO сущности пользователя по email. Email: {Email}", email);
                return null;
            }
        }

        public async Task<UserDTO> GetUserByIdAsync(Guid id)
        {
            try
            {
                User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null) return null;

                UserDTO userDto = new()
                {
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                };
                
                return userDto;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении DTO сущности пользователя по id. Id: {Id}", id);
                return null;
            }
        }

        public async Task<(bool Success, string? message)> UpdateUserAsync(User user)
        {
            try
            {
                context.Users.Update(user);
                await context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении данных о пользователе. Id: {Id}, Email: {Email}" +
                                    "FirstName: {FirstName}, LastName: {LastName}",
                    user.Id, user.Email, user.FirstName, user.LastName);
                return (false, "Ошибка при обновлении данных о пользователе");
            }
        }

        public async Task<User?> GetEntityUserByIdAsync(Guid id)
        {
            User? user;
            try
            {
                user = await context.Users.Include(u => u.UserRefreshToken).FirstOrDefaultAsync(u => u.Id == id);
                return user;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении Entity сущности пользователя по айди. UserId: {UserId}", id);
                return null;
            }
        }
    }
}