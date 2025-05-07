using WebBackend.Models.DTO;

namespace WebBackend.Repositories.Interfaces
{
    public interface IRatingRepository
    {
        public Task<(bool Success, string? Message)> PostRatingAsync(RatingDTO ratingDTO);
        public Task<RatingDTO?> GetRatingByProcessIdAsync(Guid processId);
    }
}
