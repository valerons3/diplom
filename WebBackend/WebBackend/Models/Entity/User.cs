namespace WebBackend.Models.Entity
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Guid RoleId { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public Role? UserRole { get; set; }
        public RefreshToken? UserRefreshToken { get; set; }
        public List<ProccesedData>? UserProccesedData { get; set; } = [];
        public List<Statistic>? UserStatistics { get; set; } = [];
    }
}
