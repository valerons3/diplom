using WebBackend.Models.DTO;
using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class FileService : IFileService
    {
        private readonly string uploadPath = "Uploads";
        private readonly ILogger<FileService> logger;
        private readonly HttpClient httpClient;
        public FileService(ILogger<FileService> logger)
        {
            this.logger = logger;
            httpClient = new HttpClient();
        }

        public async Task<(bool Success, string? FilePath, string? ImagePath)> DownloadAndSaveResultFilesAsync(RabbitData rabbitData)
        {
            var responseFile = await httpClient.GetAsync(rabbitData.DownloadLink);
            var responseImage = await httpClient.GetAsync(rabbitData.ImageDownloadLink);

            if (!responseFile.IsSuccessStatusCode || !responseImage.IsSuccessStatusCode)
            {
                logger.LogError("Ошибка при скачивании файлов результата. FileResponse: {FileResponse}, ImageResponse: " +
                    "{ImageResponse}", responseFile.StatusCode, responseImage.StatusCode);
                return (false, null, null);
            }


            var imageBytes = await responseImage.Content.ReadAsByteArrayAsync();
            var imageName = rabbitData.ImageDownloadLink.Split("fileName=")[^1];
            var fileBytes = await responseFile.Content.ReadAsByteArrayAsync();
            var fileName = rabbitData.DownloadLink.Split("fileName=")[^1];

            var resultSaveImage = await SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID,
                imageBytes, imageName);
            var resultSaveFile = await SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID, 
                fileBytes, fileName);

            if (!resultSaveFile.Sucess || !resultSaveImage.Sucess)
            {
                return (false, null, null);
            }

            return (true, resultSaveFile.Message, resultSaveImage.Message);
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
