using WebBackend.Models.DTO;
using WebBackend.Models.Entity;

namespace WebBackend.Services.Interfaces
{
    public interface ITokenService
    {
        public string GenerateJWTToken(User user);
        public JWTPayload? GetJWTPayload(string token);
        public string GenerateRefreshToken();
        public string GenerateSessionToken();
        public string GenerateCode();
    }
}