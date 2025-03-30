using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NeironBackend.Services.Interfaces;

namespace NeironBackend.Controllers
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

        [HttpGet("upload")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFileAsync([FromQuery] string userID, [FromQuery] string processID,
        [FromQuery] string fileName)
        {
            var resultShare = await fileService.UploadFile(userID, processID, fileName);
            if (!resultShare.Success)
            {
                return NotFound(new { message = resultShare.Message });
            }

            var fileBytes = resultShare.fileBytes;

            var fileResult = File(fileBytes, "application/octet-stream", fileName);

            _ = Task.Run(async () => await fileService.DeleteUserFolderAsync(userID));

            return fileResult;
        }
    }
}
