namespace WebBackend.Services.Interfaces
{
    public interface IFileService
    {
        public Task<(bool Success, string? Message)> SaveInputFileAsync(Guid userId, Guid processId, IFormFile file);
        public Task<(bool Success, string? Message)> SaveResultFileAsync(Guid userId, Guid processId, IFormFile file);
    }
}
