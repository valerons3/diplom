using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebBackend.Models.DTO;
using WebBackend.Repositories.Interfaces;

namespace WebBackend.Controllers
{
    [Route("api/rating")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IRatingRepository ratingRepository;

        public RatingController(IRatingRepository ratingRepository)
        {
            this.ratingRepository = ratingRepository;
        }


        [HttpGet]
        public async Task<IActionResult> GetRatingAsync([FromQuery]Guid processId)
        {
            if (processId == Guid.Empty)
            {
                return BadRequest(new { message = "Идентификатор процесса не указан" });
            }

            var rating = await ratingRepository.GetRatingByProcessIdAsync(processId);

            if (rating == null)
            {
                return NotFound(new { message = "Рейтинг для указанного процесса не найден" });
            }

            return Ok(rating);
        }

        [HttpPost]
        public async Task<IActionResult> PostRatingAsync(RatingDTO ratingDTO)
        {
            if (ratingDTO == null || ratingDTO.Grade < 1 || ratingDTO.Grade > 5)
            {
                return BadRequest(new { message = "Недопустимая оценка или отсутствующие данные" });
            }

            var result = await ratingRepository.PostRatingAsync(ratingDTO);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
