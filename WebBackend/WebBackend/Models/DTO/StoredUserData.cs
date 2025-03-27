using WebBackend.Models.Entity;

namespace WebBackend.Models.DTO
{
    public class StoredUserData
    {
        public User User { get; set; } = default!;
        public string Code { get; set; } = string.Empty;
    }
}
