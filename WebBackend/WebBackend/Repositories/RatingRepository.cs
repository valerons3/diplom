using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class RatingRepository : IRatingRepository
    {
        private readonly AppDbContext context;

        public RatingRepository(AppDbContext context)
        {
            this.context = context;
        }
        public async Task<RatingDTO?> GetRatingByProcessIdAsync(Guid processId)
        {
            var rating = await context.Ratings
                .Where(r => r.ProcessedDataId == processId)
                .FirstOrDefaultAsync();

            if (rating == null) { return null; }

            return new RatingDTO()
            {
                ProcessId = processId,
                Grade = rating.Grade,
                Comment = rating.Comment
            };
        }

        public async Task<(bool Success, string? Message)> PostRatingAsync(RatingDTO ratingDTO)
        {
            var process = await context.ProccesedDatas.FindAsync(ratingDTO.ProcessId);

            if (process == null)
                return (false, "Процесс не найден");

            if (process.RatingId != null)
                return (false, "Оценка уже существует");

            var rating = new Rating()
            {
                Id = Guid.NewGuid(),
                Grade = ratingDTO.Grade,
                Comment = ratingDTO.Comment,
                CreatedAt = DateTime.UtcNow,
                ProcessedDataId = ratingDTO.ProcessId
            };

            process.RatingId = rating.Id;

            await context.Ratings.AddAsync(rating);
            context.ProccesedDatas.Update(process);
            await context.SaveChangesAsync();

            return (true, "Оценка успешно оставлена");
        }
    }
}
