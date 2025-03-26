using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebBackend.Configurations;
using WebBackend.Data;
using WebBackend.Models.DTO;
using WebBackend.Models.Entity;
using WebBackend.Services.Interfaces;

namespace WebBackend.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtSettings jwtSettings;
        private readonly AppDbContext context;
        public TokenService(IOptions<JwtSettings> jwtSettings, AppDbContext context)
        {
            this.jwtSettings = jwtSettings.Value;
            this.context = context;
        }

        public string GenerateCode()
        {
            byte[] bytes = new byte[4]; 
            RandomNumberGenerator.Fill(bytes);
            int number = BitConverter.ToInt32(bytes, 0) % 900000 + 100000; 
            return Math.Abs(number).ToString();
        }

        public string GenerateJWTToken(User user)
        {
            var role = context.Roles.FirstOrDefault(r => r.Id == user.RoleId);

            if (role == null)
            {
                throw new Exception("Роль пользователя не найдена!");
            }

            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),   
                new Claim("role", role.Name)           
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenLifetime),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }



        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32]; 
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        public string GenerateSessionToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }
    }
}