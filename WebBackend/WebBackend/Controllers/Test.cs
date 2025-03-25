using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace WebBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Test : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SendEmail([FromQuery] string email, [FromQuery] string message)
        {
            string smtpServer = "smtp.mail.ru";
            int smtpPort = 587;
            string smtpUsername = "sardin-03@mail.ru";
            string smtpPassword = "SQQW3DB1vtsg3PRDZC5U";

            using (SmtpClient smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = true;

                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(smtpUsername);
                    mailMessage.To.Add(email);
                    mailMessage.Subject = "Тест";
                    mailMessage.Body = message;

                    try
                    {
                        smtpClient.Send(mailMessage);
                        Console.WriteLine("Сообщение успешно отправлено");
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex}");
                    }
                }
            }



                return Ok();
        }
    }
}
