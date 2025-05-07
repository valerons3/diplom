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
        private readonly ILogger<RatingRepository> logger;

        public RatingRepository(AppDbContext context, ILogger<RatingRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }
        public async Task<RatingDTO?> GetRatingByProcessIdAsync(Guid processId)
        {
            Rating? rating;
            try
            {
                rating = await context.Ratings
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при извлечении данных рейтинга по айди процесса. ProcessId: {ProcessId}", processId);
                return null;
            }
        }

        public async Task<(bool Success, string? Message)> PostRatingAsync(RatingDTO ratingDTO)
        {
            ProcessedData? process;
            try
            {
                process = await context.ProccesedDatas.FindAsync(ratingDTO.ProcessId);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при добавлении оценки");
                return (false, "Ошибка при добавлении оценки");
            }
        }
    }
}
