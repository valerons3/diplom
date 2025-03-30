using NeironBackend.Models;

namespace NeironBackend.Services.Interfaces
{
    public interface IRabbitProducer
    {
        public (bool Success, string? Message) Publish(RabbitData data);
    }
}
