using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using WebBackend.Data;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Models.Enums;
using WebBackend.Repositories.Interfaces;
using WebBackend.Services.Interfaces;
using Prometheus;

namespace WebBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    [Authorize]
    public class AuthController : ControllerBase
    {

        private static readonly Counter RegistrationCounter =
            Metrics.CreateCounter("app_user_registration_total", "Количество регистраций пользователей");

        private static readonly Counter LoginCounter =
            Metrics.CreateCounter("app_user_login_total", "Количество входов пользователей");

        private readonly IRedisService redisService;
        private readonly ITokenService tokenService;
        private readonly IEmailService emailService;
        private readonly IRefreshTokenRepository refreshTokenRepository;
        private readonly IUserRepository userRepository;
        private readonly IPasswordService passwordService;
        private readonly IRoleRepository roleRepository;
        private readonly IRevokedTokenRepository revokedTokenRepository;
        private readonly ILogger<AuthController> logger;
        public AuthController(IRedisService redisService, ITokenService tokenService, IEmailService emailService,
            IRefreshTokenRepository refreshTokenRepository, IUserRepository userRepository,
            IPasswordService passwordService, IRoleRepository roleRepository,
            IRevokedTokenRepository revokedTokenRepository, ILogger<AuthController> logger)
        {
            this.redisService = redisService;
            this.tokenService = tokenService;
            this.emailService = emailService;
            this.refreshTokenRepository = refreshTokenRepository;
            this.userRepository = userRepository;
            this.passwordService = passwordService;
            this.roleRepository = roleRepository;
            this.revokedTokenRepository = revokedTokenRepository;
            this.logger = logger;
        }

        [HttpPost("refresh-jwt")]
        public async Task<IActionResult> RefreshJwtTokenAsync()
        {
            string? refreshToken = Request.Headers["Refresh-Token"].FirstOrDefault();
            string? jwtToken = Request.Headers["Authorization"]
                            .FirstOrDefault()?
                            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                            .Trim();

            if (refreshToken == null || jwtToken == null)
            {
                return BadRequest(new { message = "Нужно передать Refresh и JWT токены" });
            }

            JWTPayload? payload = tokenService.GetJWTPayload(jwtToken);
            if (payload == null) { return BadRequest(new { message = "Не валидный JWT токен" }); }


            RefreshToken? token = await refreshTokenRepository.GetRefreshTokenAsync(refreshToken);
            if (token == null)
            {
                return NotFound(new { message = "Токен не найден" });
            }
            if (refreshToken != token.token) { return BadRequest(new { message = "Не верный Refresh токен" }); }

            if (token.ExpireDate <= DateTime.UtcNow)
            {
                var deleteResult = await refreshTokenRepository.DeleteRefreshTokenAsync(token);
                if (!deleteResult.Success) { return StatusCode(500, new { message = deleteResult.Message }); }
                return Unauthorized(new { message = "Срок действия токена истёк" });
            }

            User? user = await userRepository.GetEntityUserByIdAsync(payload.Id);
            if (user == null)
            {
                return NotFound(new { message = "Пользователь не найден" });
            }

            string newJWTToken = tokenService.GenerateJWTToken(user);
            return Ok(new { JWT = newJWTToken });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUserAsync([FromBody] Login loginData)
        {
            User? user = await userRepository.GetEntityUserByEmailAsync(loginData.Email);
            if (user == null) { return BadRequest(new { message = "Не верный логин" }); }

            bool passwordIsCorrect = passwordService.VerifyPassword(loginData.Password, user.PasswordHash);
            if (!passwordIsCorrect)
            {
                return BadRequest(new { message = "Не верный пароль" });
            }

            string jwtToken = tokenService.GenerateJWTToken(user);
            string refreshToken = tokenService.GenerateRefreshToken();

            if (user.UserRefreshToken == null)
            {
                RefreshToken newRefreshToken = new RefreshToken()
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    token = refreshToken,
                    ExpireDate = DateTime.UtcNow.AddDays(20)
                };
                var result = await refreshTokenRepository.PostRefreshTokenAsync(newRefreshToken);
                if (!result.Success)
                {
                    return StatusCode(500, new { message = result.Message });
                }
                LoginCounter.Inc();
                return Ok(new { JWT = jwtToken, Refresh = refreshToken });
            }
            else
            {
                var resultChangeRefresh =
                    await refreshTokenRepository.ChangeRefreshTokenByUserIdAsync(user.Id, refreshToken);
                if (!resultChangeRefresh.Success) { return StatusCode(500, new { message = resultChangeRefresh.Message });
                }
                LoginCounter.Inc();
                return Ok(new { JWT = jwtToken, Refresh = refreshToken });
            }
        }

        [HttpPost("exit")]
        public async Task<IActionResult> ExitUserAsync()
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
            if (payload == null) { return BadRequest(new { message = "Не валидный JWT токен" }); }

            User? user = await userRepository.GetEntityUserByIdAsync(payload.Id);
            if (user == null) { return BadRequest(new { message = "Пользователь не найден" }); }

            var resultDeleteRefresh = await refreshTokenRepository.DeleteRefreshTokenAsync(user.UserRefreshToken);
            if (!resultDeleteRefresh.Success) { return StatusCode(500, new { message = resultDeleteRefresh.Message }); }

            var revokeResult = await revokedTokenRepository.PostJWTTokenAsync(jwtToken);
            if (!revokeResult.Success)
            {
                return StatusCode(500, new { message = "Ошибка при отзыве токена" });
            }

            return Ok(new
            {
                success = true,
                message = "Выход выполнен успешно",
                timestamp = DateTime.UtcNow
            });
        }


        [HttpPost("confirm-code")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmCodeAsync([FromBody] ConfirmCode confirmData)
        {
            EmailVerificationStatus status = await redisService.CheckEmailCodeAsync(confirmData.Token, confirmData.Code);

            switch (status)
            {
                case EmailVerificationStatus.NotFound:
                    return NotFound(new { message = "Код истёк, запросите заново" });
                case EmailVerificationStatus.CodeInvalid:
                    return BadRequest(new { message = "Не верный код подтверждения" });
                case EmailVerificationStatus.CodeValid:
                    var result = await PostUserAsync(confirmData.Token);
                    if (result.Success) { RegistrationCounter.Inc(); return Ok(new { JWT = result.jwtToken, Refresh = result.refreshToken }); }
                    else { return StatusCode(500, new { result.message }); }
                default:
                    return StatusCode(500, new { message = "Неизвестная ошибка" });
            }
        }

        private async Task<(bool Success, string? message, string? jwtToken, string? refreshToken)> PostUserAsync(string token)
        {
            User? user = await redisService.GetUserDataAsync(token);
            if (user == null) { return (false, "Неизвестная ошибка", null, null); }

            var jwtToken = tokenService.GenerateJWTToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();

            var resultPostUser = await userRepository.PostUserAsync(user);
            if (!resultPostUser.Success) { return (false, resultPostUser.message, null, null); }

            RefreshToken tokenRef = new RefreshToken()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                token = refreshToken,
                ExpireDate = DateTime.UtcNow.AddDays(20)
            };
            var resultPostRefreshToken = await refreshTokenRepository.PostRefreshTokenAsync(tokenRef);
            if (!resultPostRefreshToken.Success) { return (false, resultPostRefreshToken.Message, null, null); }

            await redisService.DeleteDataAsync(token);

            return (true, null, jwtToken, refreshToken);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterUserAsync(UserDTO userDTO)
        {

            Guid? roleId = await roleRepository.GetIdRoleByNameAsync(userDTO.Role);
            if (roleId == null)
            {
                return BadRequest(new { message = "Такой роли не существует" });
            }

            var existingUser = await userRepository.CheckUserExistsAsync(userDTO.Email);
            if (existingUser.Success)
            {
                return BadRequest(new { existingUser.message });
            }

            string token = tokenService.GenerateSessionToken();
            string code = tokenService.GenerateCode();

            var codeSended = await emailService.SendEmailAsync(userDTO.Email, code);
            if (!codeSended.Success)
            {
                return BadRequest(new
                {
                    message = codeSended.message ?? "Ошибка при отправке кода подтверждения"
                });
            }


            User user = new User()
            {
                Id = Guid.NewGuid(),
                Email = userDTO.Email,
                FirstName = userDTO.FirstName,
                LastName = userDTO.LastName,
                PasswordHash = passwordService.HashPassword(userDTO.Password),
                RoleId = roleId.Value
            };
            var userDataIsCached = await redisService.PostUserDataAsync(user, token, code);
            if (!userDataIsCached.Success)
            {
                return StatusCode(500, new { userDataIsCached.message });
            }

            return Ok(new { sessionToken = token });
        }
    }
}