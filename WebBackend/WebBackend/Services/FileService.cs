using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class FileService : IFileService
    {
        private readonly string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        public async Task<(bool Success, string? Message, byte[]? fileBytes)> UploadFile(string userID, string processID, string fileName)
        {
            string filePath = Path.Combine(uploadPath, userID, processID, "Input", fileName);
            if (!File.Exists(filePath))
            {
                return (false, "Файл не найден", null);
            }
            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return (true, null, fileBytes);
        }
        public async Task<(bool Success, string? Message)> SaveInputFileAsync(Guid userId, Guid processId, IFormFile file)
        {
            try
            {
                string userProcessInputPath = Path.Combine(uploadPath, userId.ToString(), processId.ToString(), "Input");

                Directory.CreateDirectory(userProcessInputPath);

                string inputFilePath = Path.Combine(userProcessInputPath, file.FileName);

                await using var stream = new FileStream(inputFilePath, FileMode.Create, 
                    FileAccess.Write, FileShare.None);
                await file.CopyToAsync(stream);

                return (true, inputFilePath);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }
}
