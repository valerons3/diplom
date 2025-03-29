using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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
        private readonly IRabbitService rabbitService;
        private readonly string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
        public ProcessController(IFileService fileService, ITokenService tokenService,
            IProcessedDataRepository processedDataRepository, IRabbitService rabbitService)
        {
            this.fileService = fileService;
            this.tokenService = tokenService;
            this.processedDataRepository = processedDataRepository;
            this.rabbitService = rabbitService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не передан" });

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
            if (!resultSaveFile.Success) { return StatusCode(500, new { message = resultSaveFile.Message }); }

            ProcessedData processData = new ProcessedData()
            {
                Id = processId,
                Status = ProcessStatus.Processing,
                UserId = payload.Id,
                InputData = resultSaveFile.Message,
                ResultData = null,
                ProcessingTime = null,
                CommentResult = null,
                RatingId = null,
                CreatedAt = DateTime.UtcNow
            };

            var resultSaveProcessData = await processedDataRepository.PostProcessDataAsync(processData);
            if (!resultSaveProcessData.Sucess) {
                return StatusCode(500,
                new { message = "Ошибка при сохранении данных" }); }

            RabbitData rabbitData = new RabbitData()
            {
                UserID = payload.Id,
                ProcessID = processData.Id,
                ProcessingTime = null,
                DownloadLink = "залупа"
            };
            var publishResult = await rabbitService.PublishAsync(rabbitData);
            if (!publishResult.Success)
            {
                return StatusCode(500, new { message = publishResult.Message });
            }

            return Ok(new { message = "Данные отправлены на обработку" });
        }
    }

}
