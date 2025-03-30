namespace NeironBackend.Configurations
{
    public class RabbitmqSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string SenderQueue { get; set; }
        public string VirtualHost { get; set; }
        public string ReceiverQueue { get; set; }
    }
}
