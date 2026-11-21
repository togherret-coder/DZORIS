
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer.Framework.Content;

namespace MiniHttpServer.Framework.Handlers
{
    public class StaticFilesHandler : RequestHandler
    {
        private readonly string _root;

        public StaticFilesHandler()
        {
            string directory = ConfigurationManager.Instance
                .Settings.PublicDirectoryPath;

            _root = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, directory));
        }

        public override async Task HandleAsync(
            HttpListenerContext context)
        {
            string urlPath = context.Request.Url?.AbsolutePath ?? "/";

            string prefix = "/" +
                ConfigurationManager.Instance.Settings.Path.Trim('/') + "/";

            if (!urlPath.StartsWith(
                prefix, StringComparison.OrdinalIgnoreCase))
            {
                await PassNextAsync(context);
                return;
            }

            string relativePath;

            try
            {
                relativePath = Uri.UnescapeDataString(
                    urlPath.Substring(prefix.Length));

                if (relativePath.Length == 0 ||
                    relativePath.EndsWith('/'))
                {
                    relativePath += "index.html";
                }

                string filePath = Path.GetFullPath(
                    Path.Combine(_root,
                        relativePath.Replace(
                            '/', Path.DirectorySeparatorChar)));

                string rootPrefix =
                    Path.TrimEndingDirectorySeparator(_root)
                    + Path.DirectorySeparatorChar;

                if (!filePath.StartsWith(
                    rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = 403;
                    context.Response.Close();
                    return;
                }

                if (!File.Exists(filePath))
                {
                    await PassNextAsync(context);
                    return;
                }

                byte[] data = await File.ReadAllBytesAsync(filePath);

                context.Response.StatusCode = 200;
                context.Response.ContentType =
                    MimeTypeProvider.GetMimeType(filePath);
                context.Response.ContentLength64 = data.Length;
                context.Response.Headers["Server"] = "MiniHttpServer/1.0";

                await context.Response.OutputStream.WriteAsync(data);
                context.Response.Close();

                Console.WriteLine($"Отправлен файл: {relativePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки файла: {ex.Message}");

                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                    // Ответ мог быть уже закрыт.
                }
            }
        }

        private async Task PassNextAsync(HttpListenerContext context)
        {
            if (Next != null)
            {
                await Next.HandleAsync(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
    }
}
