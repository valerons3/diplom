using WebBackend.Models.Enums;

namespace WebBackend.Models.DTO
{
    public class ProcessDataDTO
    {
        public Guid Id { get; set; }
        public ProcessStatus Status { get; set; }
        public string InputData { get; set; } = string.Empty;
        public string? ResultData { get; set; } = null!;
        public string? PhaseImage { get; set; } = null!;
        public TimeSpan? ProcessingTime { get; set; } = null!;
        public string ProcessMethod { get; set; } = string.Empty;
        public string? CommentResult { get; set; } = null!;
        public Guid? RatingId { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
