using WebBackend.Models.Entity;
using WebBackend.Models.Enums;

namespace WebBackend.Repositories.Interfaces
{
    public interface IProcessedDataRepository
    {
        public Task<(bool Sucess, string? message)> PostProcessDataAsync(ProcessedData processedData);
        public Task<(bool Sucess, string? message)> ChangeProcessDataAsync(ProcessStatus status, string? resultData,
            TimeSpan? processingTime, Guid processId);
    }
}
