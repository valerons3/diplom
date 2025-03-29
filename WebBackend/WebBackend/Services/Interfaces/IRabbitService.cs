using WebBackend.Models.DTO;

namespace WebBackend.Services.Interfaces
{
    public interface IRabbitService
    {
        public Task<(bool Success, string? Message)> PublishAsync(RabbitData data);
    }
}
