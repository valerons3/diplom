using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Repositories.Interfaces;
using WebBackend.Services.Interfaces;

namespace WebBackend.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly ITokenService tokenService;

    public UsersController(IUserRepository userRepository, ITokenService tokenService)
    {
        this.userRepository = userRepository;
        this.tokenService = tokenService;
    }
    
    /// <summary>
    /// Изменение FirstName и LastName пользователя
    /// </summary>
    /// <returns>Код HTTP</returns>
    /// <response code="400">Проблемы с JWT (не передан/не валиден). Ответ: JSON { "message" = message } </response>
    /// <response code="500">Сервер гг. Ответ: JSON { "message" = message } </response>
    /// <response code="404">Пользователь не найден. Ответ: JSON { "message" = message}</response>
    /// <response code="200">Данные обновлены. Ответ: JSON { "message" = message}</response>
    [HttpPatch]
    public async Task<IActionResult> ChangeUserDataAsync([FromBody]UserShortDTO userShortDTO)
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
        
        User? user = await userRepository.GetEntityUserByIdAsync(payload.Id);

        if (user == null)
        {
            return NotFound(new { message = "Пользователь не найден" });
        }
        
        user.FirstName = userShortDTO.FirstName;
        user.LastName = userShortDTO.LastName;

        var resultUpdate = await userRepository.UpdateUserAsync(user);
        if (!resultUpdate.Success)
        {
            return StatusCode(500, new { message = resultUpdate.message });
        }

        return Ok(new { message = "Данные успешно обновлены" });
    }
    
    /// <summary>
    /// Получение информации о пользователе по email
    /// </summary>
    /// <returns>Код HTTP</returns>
    /// <response code="404">Пользователь не найден. Ответ: JSON { "message" = message } </response>
    /// <response code="200">Данные пользователя</response>
    [HttpGet("emailinfo")]
    public async Task<IActionResult> GetUserByEmailAsync([FromQuery] string email)
    {
        UserDTO? user = await userRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = "Пользователь не найден " });
        }

        return Ok(user);
    }
    
    /// <summary>
    /// Получение информации о пользователе по JWT
    /// Обязательно передать JWT в заголовке { 'Authorization': 'Bearer token' }
    /// </summary>
    /// <returns>Код HTTP</returns>
    /// <response code="400">Проблемы с JWT (не передан/не валиден). Ответ: JSON { "message" = message } </response>
    /// <response code="404">Пользователь не найден. Ответ: JSON { "message" = message}</response>
    /// <response code="200">Данные пользователя</response>
    [HttpGet("idinfo")]
    public async Task<IActionResult> GetUserByIdAsync()
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

        UserDTO? user = await userRepository.GetUserByIdAsync(payload.Id);

        if (user == null)
        {
            return NotFound(new { message = "Пользователь не найден" });
        }

        return Ok(user);
    }
}