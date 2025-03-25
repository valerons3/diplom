namespace WebBackend.Models.Entity
{
    public class ProccesedData
    {
        public Guid Id { get; set; }
        public bool AtWork { get; set; }
        public Guid UserId { get; set; }
        public string InputData { get; set; } = string.Empty;
        public string? ResultData { get; set; } = string.Empty;
        public Guid? RatingId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Rating? Rating { get; set; }
    }
}
