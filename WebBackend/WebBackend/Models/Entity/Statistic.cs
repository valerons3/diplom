namespace WebBackend.Models.Entity
{
    public class Statistic
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}