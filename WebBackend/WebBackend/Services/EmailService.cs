using System.Net.Mail;
using System.Net;
using WebBackend.Configurations;
using WebBackend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace WebBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings settings;
        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            this.settings = smtpSettings.Value;
        }

        public async Task<(bool Success, string? message)> SendEmailAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return (false, "Некорректный email адрес");
            }

            string smtpServer = settings.Server;
            int smtpPort = settings.Port;
            string smtpUsername = settings.Username;
            string smtpPassword = settings.Password;

            try
            {
                using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(smtpUsername);
                        mailMessage.To.Add(email); 
                        mailMessage.Subject = "Код подтверждения";
                        mailMessage.Body = code;

                        await smtpClient.SendMailAsync(mailMessage);
                        return (true, null);
                    }
                }
            }
            catch (SmtpException ex)
            {
                return (false, $"Ошибка SMTP: {ex.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка отправки: {ex.Message}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}