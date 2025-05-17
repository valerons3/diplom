using WebBackend.Models.DTO;
using WebBackend.Models.Entity;

namespace WebBackend.Repositories.Interfaces
{
    public interface IUserRepository
    {
        public Task<(bool Success, string? message)> PostUserAsync(User user);
        public Task<Register?> GetUserInfoByIdAsync(Guid id);
        public Task<(bool Success, string? message)> CheckUserExistsAsync(string email);
        public Task<User?> GetEntityUserByIdAsync(Guid id);
        public Task<User?> GetEntityUserByEmailAsync(string email);
        public Task<UserDTO> GetUserByEmailAsync(string email);
        public Task<UserDTO> GetUserByIdAsync(Guid id);
        public Task<(bool Success, string? message)> UpdateUserAsync(User user);
    }
}