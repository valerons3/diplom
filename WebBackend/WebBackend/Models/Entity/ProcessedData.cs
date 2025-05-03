using WebBackend.Models.Enums;

namespace WebBackend.Models.Entity
{
    public class ProcessedData
    {
        public Guid Id { get; set; }
        public ProcessStatus Status { get; set; }
        public Guid UserId { get; set; }
        public string InputData { get; set; } = string.Empty;
        public string? ResultData { get; set; } = null!;
        public string? PhaseImage { get; set; } = null!; // новое поле
        public TimeSpan? ProcessingTime { get; set; } = null!;
        public string ProcessMethod { get; set; } = string.Empty;
        public string? CommentResult { get; set; } = null!;
        public Guid? RatingId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Rating? Rating { get; set; }
    }
}