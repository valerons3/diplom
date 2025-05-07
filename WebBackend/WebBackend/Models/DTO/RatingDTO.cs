namespace WebBackend.Models.DTO
{
    public class RatingDTO
    {
        public Guid ProcessId { get; set; }
        public int Grade { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
