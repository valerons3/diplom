namespace WebBackend.Configurations
{
    public class JwtSettings
    {
        public string Key { get; set; }
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenLifetime { get; set; }
    }
}
