using NeironBackend.Services.Interfaces;

namespace NeironBackend.Services
{
    public class FileService : IFileService
    {
        private readonly string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        public async Task<(bool Success, string? Message, byte[]? fileBytes)> UploadFile(string userID, string processID, string fileName)
        {
            string filePath = Path.Combine(uploadPath, userID, processID, "Result", fileName);
            if (!File.Exists(filePath))
            {
                return (false, "Файл не найден", null);
            }
            var fileBytes = await File.ReadAllBytesAsync(filePath);

            return (true, null, fileBytes);
        }

        public async Task DeleteUserFolderAsync(string userId)
        {
            var userFolderPath = Path.Combine(uploadPath, userId);

            if (Directory.Exists(userFolderPath))
            {
                await Task.Run(() => Directory.Delete(userFolderPath, true));
            }
            else
            {
            }
        }

    }
}
