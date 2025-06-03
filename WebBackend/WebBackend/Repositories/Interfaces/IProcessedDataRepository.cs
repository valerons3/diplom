using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Models.Enums;

namespace WebBackend.Repositories.Interfaces
{
    public interface IProcessedDataRepository
    {
        public Task<(bool Sucess, string? message)> PostProcessDataAsync(ProcessedData processedData);
        public Task<(bool Sucess, string? message)> ChangeProcessDataAsync(ProcessStatus status, string? resultData,
            string? imageData, TimeSpan? processingTime, Guid processId);
        public Task<(bool Success, string? Message)> ChangeDataIfSuccessAsync(RabbitData data, string filePath,
            string resultImagePath, string inputImagePath);
        public Task<(bool Success, string? Messsage)> ChangeDataIfNotSuccess(RabbitData data);
        public Task<ProcessedData?> GetProcessDataByIdAsync(Guid processId);
        public Task<List<ProcessedData>?> GetAllUserProcessedData(Guid userID);
    }
}
