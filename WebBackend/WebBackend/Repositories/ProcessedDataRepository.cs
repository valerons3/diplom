using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.DTO;
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
                existingProcessData.ResultPhaseImage = imageData;
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

        public async Task<(bool Success, string? Message)> ChangeDataIfSuccessAsync(RabbitData data, string filePath,
            string resultImagePath, string inputImagePath)
        {
            try
            {
                var existingProcess = await context.ProccesedDatas
                    .FirstOrDefaultAsync(p => p.Id == data.ProcessID);

                if (existingProcess == null)
                {
                    return (false, "Данных о процессе не существует");
                }

                existingProcess.Status = data.Status;
                existingProcess.ProcessingTime = data.ProcessingTime;
                existingProcess.ResultData = filePath;
                existingProcess.ResultPhaseImage = resultImagePath;
                existingProcess.InputPhaseImage = inputImagePath;

                context.ProccesedDatas.Update(existingProcess);
                await context.SaveChangesAsync();
                return (true, "Данные обновлены");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении данных о процессе. ProcessId: {ProcessId}", data.ProcessID);
                return (false, "Ошибка при обновлении данных о процессе");
            }
        }

        public async Task<(bool Success, string? Messsage)> ChangeDataIfNotSuccess(RabbitData data)
        {
            try
            {
                var existingProcess = await context.ProccesedDatas
                    .FirstOrDefaultAsync(p => p.Id == data.ProcessID);

                if (existingProcess == null)
                {
                    return (false, "Данных о процессе не существует");
                }

                existingProcess.Status = data.Status;
                existingProcess.CommentResult = "Ошибка при обработке данных";

                context.ProccesedDatas.Update(existingProcess);
                await context.SaveChangesAsync();
                return (true, "Данные обновлены");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при обновлении данных о процессе. ProcessId: {ProcessId}", data.ProcessID);
                return (false, "Ошибка при обновлении данных о процессе");
            }
        }
    }
}
