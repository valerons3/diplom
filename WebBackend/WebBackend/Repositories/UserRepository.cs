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
        public UserRepository(AppDbContext context)
        {
            this.context = context;
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
                return (false, null);
            }
        }

        public async Task<UserDTO?> GetUserInfoByIdAsync(Guid id)
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

                return new UserDTO
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
                return (false, "Ошибка при регистрации пользователя");
            }
        }

        public async Task<User?> GetEntityUserByEmailAsync(string email)
        {
            User? user = await context.Users.Include(u => u.UserRefreshToken).FirstOrDefaultAsync(u => u.Email == email);
            return user;
        }

        public async Task<User?> GetEntityUserByIdAsync(Guid id)
        {
            User? user = await context.Users.Include(u => u.UserRefreshToken).FirstOrDefaultAsync(u => u.Id == id);
            return user;
        }
    }
}