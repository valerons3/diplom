using WebBackend.Models.Entity;

namespace WebBackend.Services.Interfaces
{
    public interface ITokenService
    {
        public string GenerateJWTToken(User user);
        public string GenerateRefreshToken();
        public string GenerateSessionToken();
        public string GenerateCode();
    }
}
