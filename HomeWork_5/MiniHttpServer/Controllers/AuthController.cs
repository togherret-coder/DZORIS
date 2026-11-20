
using System;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Responses;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers
{
    [Controller]
    public class AuthController
    {
        private readonly IEmailService _emailService;

        public AuthController()
        {
            _emailService = new EmailService();
        }

        [Post]
        public async Task<HttpResult> SendEmail(
            string email,
            string message)
        {
            
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                return new HttpResult(
                    400,
                    "Error: email and message are required");
            }

            try
            {
                
                await _emailService.SendEmailAsync(
                    email, message);

                return new HttpResult(
                    200,
                    "Email sent successfully!");
            }
            catch (Exception ex)
            {
                
                Console.WriteLine(
                    $"Ошибка отправки письма: {ex}");

                return new HttpResult(
                    502,
                    "Error: email service unavailable");
            }
        }
    }
}
