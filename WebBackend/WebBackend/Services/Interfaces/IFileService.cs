using WebBackend.Models.DTO;

namespace WebBackend.Services.Interfaces
{
    public interface IFileService
    {
        public Task<(bool Success, string? Message)> SaveInputFileAsync(Guid userId, Guid processId, IFormFile file);
        public Task<(bool Sucess, string? Message)> SaveResultFileAsync(Guid userId, Guid processId, 
            byte[] fileBytes, string fileName);
        public Task<(bool Success, string? FilePath, string? ResultImagePath, string? InputImagePath)> DownloadAndSaveResultFilesAsync(RabbitData data);
    }
}
