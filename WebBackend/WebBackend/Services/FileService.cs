using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class FileService : IFileService
    {
        private readonly string uploadPath = "Uploads";
        private readonly ILogger<FileService> logger;
        public FileService(ILogger<FileService> logger)
        {
            this.logger = logger;
        }

        public async Task<(bool Sucess, string? Message)> SaveResultFileAsync(Guid userId, Guid processId,
            byte[] fileBytes, string fileName)
        {
            var resultPath = Path.Combine(uploadPath, userId.ToString(), processId.ToString(), "Result");
            var filePath = Path.Combine(resultPath, fileName);
            try
            {
                Directory.CreateDirectory(resultPath);
                await File.WriteAllBytesAsync(filePath, fileBytes);
                return (true, filePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при сохранении файла результата. ResultFilePath: {ResultFilePath}", filePath);
                return (false, null);
            }
        }

        public async Task<(bool Success, string? Message)> SaveInputFileAsync(Guid userId, Guid processId, IFormFile file)
        {
            string userProcessInputPath = Path.Combine(uploadPath, userId.ToString(), processId.ToString(), "Input");
            string inputFilePath = Path.Combine(userProcessInputPath, file.FileName);
            try
            {
                
                Directory.CreateDirectory(userProcessInputPath);
                
                await using var stream = new FileStream(inputFilePath, FileMode.Create,
                    FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream);

                return (true, inputFilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при сохранении входного файла. InputFilePath: {InputFilePath}", inputFilePath);
                return (false, ex.Message);
            }
        }

    }
}
