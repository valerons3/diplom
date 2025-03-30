namespace NeironBackend.Models
{
    public class RabbitData
    {
        public Guid UserID { get; set; }
        public Guid ProcessID { get; set; }
        public TimeSpan? ProcessingTime { get; set; } = null;
        public ProcessStatus Status { get; set; } = ProcessStatus.Processing;
        public string DownloadLink { get; set; }
    }

    public enum ProcessStatus
    {
        Processing, // В обработке
        Success,    // Успешно обработано
        Failed      // Ошибка
    }
}
