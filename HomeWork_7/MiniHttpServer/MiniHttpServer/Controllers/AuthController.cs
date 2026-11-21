using MiniHttpServer.Framework.Controllers;
using System;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Results;
using MiniHttpServer.Services;

namespace MiniHttpServer.Controllers
{
    [Controller]
    public class AuthController : BaseController
    {
        private readonly IEmailService _emailService;

        public AuthController()
        {
            _emailService = new EmailService();
        }

        [Post]

        public async Task<IHttpResult> SendEmail(
    string email,
    string message)
        {
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(message))
            {
                return Json(new
                {
                    Error = "email and message are required"
                }, 400);
            }

            try
            {
                await _emailService.SendEmailAsync(
                    email, message);

                return Json(new
                {
                    Message = "Email sent successfully!"
                }, 200);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Ошибка отправки письма: {ex}");

                return Json(new
                {
                    Error = "email service unavailable"
                }, 502);
            }
        }

    }
}

