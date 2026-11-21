
using System;
using System.Net;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Handlers
{
    public class LoggingHandler : RequestHandler
    {
        public override async Task HandleAsync(
            HttpListenerContext context)
        {
            Console.WriteLine(
                $"[{context.Request.HttpMethod}] " +
                $"{context.Request.Url?.AbsolutePath}");

            if (Next != null)
            {
                await Next.HandleAsync(context);
            }
        }
    }
}
