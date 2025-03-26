using System.Net.Mail;
using System.Net;
using WebBackend.Configurations;
using WebBackend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace WebBackend.Services
{
    public class EmailSerice : IEmailService
    {
        private readonly SmtpSettings settings;
        public EmailSerice(IOptions<SmtpSettings> smtpSettings)
        {
            this.settings = smtpSettings.Value;
        }

        public async Task<(bool Success, string? message)> SendEmailAsync(string email, string code)
        {
            string smtpServer = settings.Server;
            int smtpPort = settings.Port;
            string smtpUsername = settings.Username;
            string smtpPassword = settings.Password;

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;

                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(email);
                    mailMessage.Subject = "Код подтверждения";
                    mailMessage.Body = code;

                    try
                    {
                        await smtpClient.SendMailAsync(mailMessage); 
                        return (true, null); 
                    }
                    catch (Exception ex)
                    {
                        return (false, ex.Message); 
                    }
                }
            }
        }
    }
}
