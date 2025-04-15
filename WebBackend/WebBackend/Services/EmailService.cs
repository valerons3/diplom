using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using WebBackend.Services.Interfaces;
using WebBackend.Configurations;

namespace WebBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings settings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            settings = smtpSettings.Value;
        }

        public async Task<(bool Success, string? message)> SendEmailAsync(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Email не может быть пустым");
            }

            email = email.Trim();

            if (!IsValidEmail(email))
            {
                return (false, $"Некорректный email адрес: {email}");
            }

            try
            {
                using var smtpClient = new SmtpClient(settings.Server, settings.Port)
                {
                    Credentials = new NetworkCredential(settings.Username, settings.Password),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.Username),
                    Subject = "Код подтверждения",
                    Body = code,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return (true, null);
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
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}