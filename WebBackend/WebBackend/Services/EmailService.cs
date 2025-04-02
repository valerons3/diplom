using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using WebBackend.Configurations;
using WebBackend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace WebBackend.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _settings = smtpSettings.Value;
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
                using var smtpClient = new SmtpClient(_settings.Server, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.Username),
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