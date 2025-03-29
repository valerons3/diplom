namespace WebBackend.Models.DTO
{
    public class RabbitData
    {
        public Guid UserID { get; set; }
        public Guid ProcessID { get; set; }
        public TimeSpan? ProcessingTime { get; set; } = null;
        public string DownloadLink { get; set; }
    }
}
