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

        public async Task<(bool Success, string? FilePath, string? ResultImagePath, string? InputImagePath)> DownloadAndSaveResultFilesAsync(RabbitData rabbitData)
        {
            var responseFile = await httpClient.GetAsync(rabbitData.DownloadLink);
            var responseResultImage = await httpClient.GetAsync(rabbitData.ResultImageDownloadLink);
            var responceInputImage = await httpClient.GetAsync(rabbitData.InputImageDownloadLink);

            if (!responseFile.IsSuccessStatusCode || !responseResultImage.IsSuccessStatusCode || !responceInputImage.IsSuccessStatusCode)
            {
                logger.LogError("Ошибка при скачивании файлов результата. FileResponse: {FileResponse}, ImageResponse: " +
                    "{ImageResponse}", responseFile.StatusCode, responseResultImage.StatusCode);
                return (false, null, null, null);
            }


            var resultImageBytes = await responseResultImage.Content.ReadAsByteArrayAsync();
            var resultImageName = rabbitData.ResultImageDownloadLink.Split("fileName=")[^1];

            var inputImageBytes = await responceInputImage.Content.ReadAsByteArrayAsync();
            var inputImageName = rabbitData.InputImageDownloadLink.Split("fileName=")[^1];
            
            var fileBytes = await responseFile.Content.ReadAsByteArrayAsync();
            var fileName = rabbitData.DownloadLink.Split("fileName=")[^1];

            var resultSaveResultImage = await SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID,
                resultImageBytes, resultImageName);
            var resultSaveInputImage = await SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID,
                inputImageBytes, inputImageName);
            var resultSaveFile = await SaveResultFileAsync(rabbitData.UserID, rabbitData.ProcessID, 
                fileBytes, fileName);

            if (!resultSaveFile.Sucess || !resultSaveResultImage.Sucess || !resultSaveInputImage.Sucess)
            {
                return (false, null, null, null);
            }

            return (true, resultSaveFile.Message, resultSaveResultImage.Message, resultSaveInputImage.Message);
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
