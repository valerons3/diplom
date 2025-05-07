namespace WebBackend.Models.Entity
{
    public class Statistic
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}