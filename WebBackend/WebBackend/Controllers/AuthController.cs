using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBackend.Data;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Services.Interfaces;

namespace WebBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IRedisService redisService;
        private readonly ITokenService tokenService;
        private readonly IEmailService emailService;
        private readonly AppDbContext context;
        public AuthController(IRedisService redisService, ITokenService tokenService, IEmailService emailService,
            AppDbContext context)
        {
            this.redisService = redisService;
            this.tokenService = tokenService;
            this.emailService = emailService;
            this.context = context;
        }

        [HttpPost("checkJWT")]
        public async Task<IActionResult> GetJWT([FromBody] UserDTO userDTO)
        {
            var role = context.Roles.FirstOrDefault(r => r.Id == userDTO.RoleId);
            User user = new User()
            {
                Id = Guid.NewGuid(),
                Email = userDTO.Email,
                FirstName = userDTO.FirstName,
                LastName = userDTO.LastName,
                PasswordHash = userDTO.PasswordHash,
                RoleId = userDTO.RoleId,
                Created = DateTime.UtcNow,
                UserRole = role
            };

            var token = tokenService.GenerateJWTToken(user);
            return Ok(token);
        }

        [HttpPost]
        public async Task<IActionResult> PostUser([FromBody] UserDTO userDTO)
        {
            User user = new User()
            {
                Id = Guid.NewGuid(),
                Email = userDTO.Email,
                FirstName = userDTO.FirstName,
                LastName = userDTO.LastName,
                PasswordHash = userDTO.PasswordHash,
                RoleId = userDTO.RoleId,
                Created = DateTime.UtcNow
            };
            string code = tokenService.GenerateCode();
            string token = tokenService.GenerateSessionToken();

            var result = await emailService.SendEmailAsync(userDTO.Email, code);
            if (!result.Success)
            {
                return BadRequest(result.message);
            }
            await redisService.PostUserDataAsync(user, token, code);
            return Ok(token);
        }

        [HttpGet("ckeckcode")]
        public async Task<IActionResult> CheckCode([FromQuery] string token, [FromQuery]string code)
        {
            var result =  await redisService.CheckEmailCodeAsync(token, code);
            return Ok(result);
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetUserData([FromQuery] string token)
        {
            User user = await redisService.GetUserDataAsync(token);
            return Ok(user);
        }
        //[HttpPost("register")]
        //public async Task<IActionResult> RegisterUserAsync([FromBody] UserDTO userDTO)
        //{
        //    userDTO маппиться в User user

        //    Генерируется токен сесии token, генерируется код code


        //    token вместе с code и user сохранятеся в Redis

        //     token сессии отправляется обратно
        //}

        //[HttpPost("confirm")]
        //public async Task<IActionResult> ConfirmUserCodeAsync([FromBody] ConfirmCode confirm)
        //{
        //    Ищем в Redis по токену сессии и проверяем код

        //    Если код верный, то достаём всю запись из redis в User user

        //     Генерируем Jwt токен и Refresh токен, записываем в базу данных


        //}

        //[HttpPost("refresh")]
        //public async Task<IActionResult> RefreshJwtTokenAsync()
        //{
        //    Здесь получаем JWT токен вместе с refresh токеном
        //     Извлекаем из JWT токена айди пользователя
        //     Находим refresh токен по айди пользователя
        //     Если токен не истёк, то генерируем новый JWT токен и отправляем
        //}

        //[HttpPost("login")]
        //public Task<IActionResult> LoginUserAsync([FromBody] Login loginData)
        //{
        //    Проверяем пользователя в базе данных
        //    Сверяем пароль, если правильный генерируем пару токенов
        //    Обновляем refresh токен в базе данных
        //}

    }
}
