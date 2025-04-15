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
        public ProcessedDataRepository(AppDbContext context)
        {
            this.context = context;
        }

        public async Task<List<ProcessedData>?> GetAllUserProcessedData(Guid userID)
        {
            List<ProcessedData>? processedDatas = await context.ProccesedDatas
                .Where(p => p.UserId == userID)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
            return processedDatas;
        }

        public async Task<ProcessedData?> GetProcessDataByIdAsync(Guid processId)
        {
            ProcessedData? data = await context.ProccesedDatas.FirstOrDefaultAsync(pd => pd.Id == processId);
            return data;
        }
        public async Task<(bool Sucess, string? message)> ChangeProcessDataAsync(ProcessStatus status, string? resultData,
            TimeSpan? processingTime, Guid processId)
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
                return (false, $"Произошлая ошибка");
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
                return (false, "Ошибка при сохранении данных в базу данных");
            }
        }
    }
}
