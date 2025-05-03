using WebBackend.Models.Enums;

namespace WebBackend.Models.DTO
{
    public class RabbitData
    {
        public Guid UserID { get; set; }
        public Guid ProcessID { get; set; }
        public TimeSpan? ProcessingTime { get; set; } = null;
        public ProcessStatus Status { get; set; } = ProcessStatus.Processing;
        public string ProcessMethod { get; set; } = string.Empty;
        public string? DownloadLink { get; set; } = null!;
        public string? ImageDownloadLink { get; set; } = null!;
    }
}
