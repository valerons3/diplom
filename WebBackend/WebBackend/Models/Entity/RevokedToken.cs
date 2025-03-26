namespace WebBackend.Models.Entity
{
    public class RevokedToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    }
}