using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using WebBackend.Configurations;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Models.Enums;
using WebBackend.Repositories.Interfaces;
using WebBackend.Services.Interfaces;

namespace WebBackend.Controllers
{
    [Route("api/process")]
    [ApiController]
    [Authorize]
    public class ProcessController : ControllerBase
    {
        private readonly IFileService fileService;
        private readonly ITokenService tokenService;
        private readonly IProcessedDataRepository processedDataRepository;
        private readonly IRabbitProducerService rabbitService;
        private readonly DownloadURL downloadURL; 
        private readonly string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        public ProcessController(IFileService fileService, ITokenService tokenService,
            IProcessedDataRepository processedDataRepository, IRabbitProducerService rabbitService, 
            IOptions<DownloadURL> downloadURL)
        {
            this.fileService = fileService;
            this.tokenService = tokenService;
            this.processedDataRepository = processedDataRepository;
            this.rabbitService = rabbitService;
            this.downloadURL = downloadURL.Value;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery]string method)
        {

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не передан" });

            if (string.IsNullOrEmpty(method))
                return BadRequest(new { message = "Необходимо передать название метода"});
            if (method != "neural" && method != "classical")
                return BadRequest(new { message = "Не верное название метода" });

            string? jwtToken = Request.Headers["Authorization"]
                            .FirstOrDefault()?
                            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                            .Trim();

            if (jwtToken == null)
            {
                return BadRequest(new { message = "Нужно передать JWT токен" });
            }
            
            JWTPayload? payload = tokenService.GetJWTPayload(jwtToken);
            if (payload == null)
            {
                return BadRequest(new { message = "Не валидный JWT токен" });
            }

            Guid processId = Guid.NewGuid();

            var resultSaveFile = await fileService.SaveInputFileAsync(payload.Id, processId, file);
            Console.WriteLine($"Message: {resultSaveFile.Message}\tResult: {resultSaveFile.Success}");
            if (!resultSaveFile.Success) { return StatusCode(500, new { message = resultSaveFile.Message }); }

            ProcessedData processData = new ProcessedData()
            {
                Id = processId,
                Status = ProcessStatus.Processing,
                UserId = payload.Id,
                InputData = resultSaveFile.Message,
                ResultData = null,
                ProcessingTime = null,
                ProcessMethod = method,
                CommentResult = null,
                RatingId = null,
                CreatedAt = DateTime.UtcNow
            };

            var resultSaveProcessData = await processedDataRepository.PostProcessDataAsync(processData);
            if (!resultSaveProcessData.Sucess) {
                return StatusCode(500,
                new { message = "Ошибка при сохранении данных" }); }


            string downloadLink = $"{downloadURL.BaseUrl}userID={payload.Id}&processID={processData.Id}&fileName={file.FileName}";
            Console.WriteLine(downloadLink);
            RabbitData rabbitData = new RabbitData()
            {
                UserID = payload.Id,
                ProcessID = processData.Id,
                Status = ProcessStatus.Processing,
                ProcessingTime = null,
                ProcessMethod = method,
                DownloadLink = $"{downloadURL.BaseUrl}userID={payload.Id}&processID={processData.Id}&fileName={file.FileName}"
            };
            var publishResult = rabbitService.Publish(rabbitData);
            if (!publishResult.Success)
            {
                return StatusCode(500, new { message = publishResult.Message });
            }

            return Ok(new { message = "Данные отправлены на обработку" });
        }
    }

}
