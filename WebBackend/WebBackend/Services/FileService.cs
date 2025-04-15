using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class FileService : IFileService
    {
        private readonly string uploadPath = "Uploads";
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
