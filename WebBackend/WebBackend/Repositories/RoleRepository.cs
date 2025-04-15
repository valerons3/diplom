using WebBackend.Data;
using Microsoft.EntityFrameworkCore;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext context;
        public RoleRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<Guid?> GetIdRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            return await context.Roles
                .Where(r => EF.Functions.ILike(r.Name, roleName))
                .Select(r => (Guid?)r.Id)
                .FirstOrDefaultAsync();
        }
    }
}