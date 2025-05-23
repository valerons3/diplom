﻿namespace WebBackend.Repositories.Interfaces
{
    public interface IRevokedTokenRepository
    {
        public Task<(bool Success, string? Message)> PostJWTTokenAsync(string token);
        public Task<bool> IsTokenRevokedAsync(string token);
    }
}
