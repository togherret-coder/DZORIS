using System.Threading.Tasks;

namespace MiniHttpServer.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string message);
    }
}
