namespace WebBackend.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        public Task<Guid?> GetIdRoleByNameAsync(string roleName);
    }
}