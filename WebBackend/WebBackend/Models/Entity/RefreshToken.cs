namespace WebBackend.Models.Entity
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string token { get; set; } = string.Empty;
        public DateTime ExpireDate { get; set; }

        public User? User { get; set; }
    }
}