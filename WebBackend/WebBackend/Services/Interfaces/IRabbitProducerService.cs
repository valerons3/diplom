using WebBackend.Models.DTO;

namespace WebBackend.Services.Interfaces
{
    public interface IRabbitProducerService
    {
        public (bool Success, string? Message) Publish(RabbitData data);
    }
}
