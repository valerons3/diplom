using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebBackend.Services.Interfaces;

namespace WebBackend.Controllers
{
    [Route("api/fileshare")]
    [ApiController]
    public class FileShareController : ControllerBase
    {
        private readonly IFileService fileService;
        public FileShareController(IFileService fileService)
        {
            this.fileService = fileService;
        }
        private readonly string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        [HttpGet("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileAsync([FromQuery] string userID, [FromQuery] string processID,
            [FromQuery] string fileName)
        {
            var resultShare = await fileService.UploadFile(userID, processID, fileName);
            if (!resultShare.Success) { return NotFound(new { message = resultShare.Message }); }
            return File(resultShare.fileBytes, "application/octet-stream", fileName);
        }
    }
}
