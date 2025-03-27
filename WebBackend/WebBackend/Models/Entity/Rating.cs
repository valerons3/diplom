namespace WebBackend.Models.Entity
{
    public class Rating
    {
        public Guid Id { get; set; }
        public int Grade { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public ProcessedData? ProccesedData { get; set; }
    }
}