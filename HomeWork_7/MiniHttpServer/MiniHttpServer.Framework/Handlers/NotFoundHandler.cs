
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Handlers
{
    public class NotFoundHandler : RequestHandler
    {
        public override async Task HandleAsync(
            HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;

            string message = "404 Not Found";
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            response.StatusCode = 404;
            response.ContentType = "text/plain; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.Headers["Server"] = "MiniHttpServer/1.0";

            try
            {
                await response.OutputStream.WriteAsync(buffer);
            }
            finally
            {
                response.Close();
            }
        }
    }
}
