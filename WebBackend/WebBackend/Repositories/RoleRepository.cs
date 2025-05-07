using WebBackend.Data;
using Microsoft.EntityFrameworkCore;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext context;
        private readonly ILogger<RoleRepository> logger;
        public RoleRepository(AppDbContext context, ILogger<RoleRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<Guid?> GetIdRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            Guid? id;

            try
            {
                id = await context.Roles
                .Where(r => EF.Functions.ILike(r.Name, roleName))
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync();
                return id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении айди роли по имени. RoleName: {RoleName}", roleName);
                return null;
            }
        }
    }
}