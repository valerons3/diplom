using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Prometheus;
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
        private static readonly Counter FileUploadCounter =
            Metrics.CreateCounter("app_file_upload_total", "Количество загруженных файлов на обработку");

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

        /// <summary>
        /// Все обработки пользователя. Берётся по JWT
        /// </summary>
        /// <returns>Список обработок</returns>
        /// <response code="400">Проблемы с JWT. Ответ: JSON { "message" = message }</response>
        /// <response code="404">Список обработок не найден. Ответ: JSON { "message" = message }</response>
        /// <response code="200">Список обработок. Возвращается вместе с Id обработки</response>
        [HttpGet("userprocessdata")]
        public async Task<IActionResult> GetAllUserProcessData()
        {
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

            List<ProcessedData>? data = await processedDataRepository.GetAllUserProcessedData(payload.Id);
            if (data == null)
            {
                return NotFound();
            }

            var result = data.Select(p => new ProcessDataDTO
            {
                Id = p.Id,
                Status = p.Status,
                InputData = p.InputData,
                ResultData = p.ResultData,
                PhaseImage = p.PhaseImage,
                ProcessingTime = p.ProcessingTime,
                ProcessMethod = p.ProcessMethod,
                CommentResult = p.CommentResult,
                RatingId = p.RatingId,
                CreatedAt = p.CreatedAt
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Информация по одной обработке.
        /// Нужно передать JWT
        /// </summary>
        /// <returns>Информация об обработке</returns>
        /// <response code="404">Информация обработки не найдена. Ответ: JSON { "message" = message }</response>
        /// <response code="200">Информация об обработке. Возвращается вместе с Id обработки</response>
        [HttpGet("processdata")]
        public async Task<IActionResult> GetProcessInfoByIdAsync([FromQuery] Guid id)
        {
            ProcessedData? data = await processedDataRepository.GetProcessDataByIdAsync(id);
            if (data == null)
            {
                return NotFound();
            }

            ProcessDataDTO dataDTO = new ProcessDataDTO()
            {
                Id = id,
                Status = data.Status,
                InputData = data.InputData,
                ResultData = data.ResultData,
                PhaseImage = data.PhaseImage,
                ProcessingTime = data.ProcessingTime,
                ProcessMethod = data.ProcessMethod,
                CommentResult = data.CommentResult,
                RatingId = data.RatingId,
                CreatedAt = data.CreatedAt
            };

            return Ok(dataDTO);
        }

        /// <summary>
        /// Загрузка файла на обработку. В строке запроса указать название метода обработки
        /// 'neural'. Другой способ сокомандник зажал
        /// </summary>
        /// <returns>Метаданные</returns>
        /// <response code="400">Проблемы с JWT/не верное название метода/файл не передан. Ответ: JSON { "message" = message }</response>
        /// <response code="500">Серверу гг. Ответ: JSON { "message" = message }</response>
        /// <response code="200">Данные отправились на обработку. Ответ: JSON { "message" = message, "id" = id }</response>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string method)
        {

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Файл не передан" });

            if (string.IsNullOrEmpty(method))
                return BadRequest(new { message = "Необходимо передать название метода" });
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
                PhaseImage = null,
                ProcessingTime = null,
                ProcessMethod = method,
                CommentResult = null,
                RatingId = null,
                CreatedAt = DateTime.UtcNow
            };

            var resultSaveProcessData = await processedDataRepository.PostProcessDataAsync(processData);
            if (!resultSaveProcessData.Sucess)
            {
                return StatusCode(500,
                new { message = "Ошибка при сохранении данных" });
            }


            string downloadLink = $"{downloadURL.BaseUrl}{resultSaveFile.Message.Replace("\\", "/")}";
            RabbitData rabbitData = new RabbitData()
            {
                UserID = payload.Id,
                ProcessID = processData.Id,
                Status = ProcessStatus.Processing,
                ProcessingTime = null,
                ProcessMethod = method,
                DownloadLink = downloadLink,
                ImageDownloadLink = null
            };
            var publishResult = rabbitService.Publish(rabbitData);
            if (!publishResult.Success)
            {
                return StatusCode(500, new { message = publishResult.Message });
            }
            FileUploadCounter.Inc();
            return Ok(new { message = "Данные отправлены на обработку", id = processId });
        }
    }

}
