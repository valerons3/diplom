using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.Entity;
using WebBackend.Models.Enums;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Repositories
{
    public class ProcessedDataRepository : IProcessedDataRepository
    {
        private readonly AppDbContext context;
        private readonly ILogger<ProcessedDataRepository> logger;
        public ProcessedDataRepository(AppDbContext context, ILogger<ProcessedDataRepository> logger)
        {
            this.context = context;
            this.logger = logger;
        }

        public async Task<List<ProcessedData>?> GetAllUserProcessedData(Guid userID)
        {
            List<ProcessedData>? processedDatas;

            try
            {
                processedDatas = await context.ProccesedDatas
                .Where(p => p.UserId == userID)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
                return processedDatas;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении всех обработок пользователя. UserId: {UserId}", userID);
                return null;
            }
        }

        public async Task<ProcessedData?> GetProcessDataByIdAsync(Guid processId)
        {
            ProcessedData? data;
            try
            {
                data = await context.ProccesedDatas.FirstOrDefaultAsync(pd => pd.Id == processId);
                return data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка получения данных об обработке по айди обработки. ProcessId: {ProcessId}", processId);
                return null;
            }
        }
        public async Task<(bool Sucess, string? message)> ChangeProcessDataAsync(ProcessStatus status, string? resultData,
            string? imageData, TimeSpan? processingTime, Guid processId)
        {
            try
            {
                var existingProcessData = await context.ProccesedDatas.FirstOrDefaultAsync(pd => pd.Id == processId);
                if (existingProcessData == null)
                {
                    return (false, "Ошибка нахождения данных");
                }

                existingProcessData.Status = status;
                existingProcessData.ResultData = resultData;
                existingProcessData.PhaseImage = imageData;
                existingProcessData.ProcessingTime = processingTime;
                if (status == ProcessStatus.Failed)
                {
                    existingProcessData.CommentResult = "Ошибка при обработке данных";
                }

                context.ProccesedDatas.Update(existingProcessData);
                await context.SaveChangesAsync();

                return (true, "Данные обновлены");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при изменении данных обработки. ProcessId: {ProcessId}", processId);
                return (false, $"Ошибка при изменении данных обработки");
            }
        }
        public async Task<(bool Sucess, string? message)> PostProcessDataAsync(ProcessedData processedData)
        {
            try
            {
                await context.ProccesedDatas.AddAsync(processedData);
                await context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при сохранении данных в базу данных");
                return (false, "Ошибка при сохранении данных в базу данных");
            }
        }
    }
}
