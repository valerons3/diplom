using WebBackend.Models.Entity;

namespace WebBackend.Repositories.Interfaces
{
    public interface IProcessedDataRepository
    {
        public Task<(bool Sucess, string? message)> PostProcessDataAsync(ProcessedData processedData);
    }
}
