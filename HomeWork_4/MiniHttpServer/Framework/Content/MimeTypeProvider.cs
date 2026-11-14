
using System.IO;

namespace MiniHttpServer.Framework.Content
{
    public static class MimeTypeProvider
    {
        public static string GetMimeType(string filePath)
        {
            string extension = Path.GetExtension(filePath)
                .ToLowerInvariant();

            return extension switch
            {
                ".html" or ".htm" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "text/javascript; charset=utf-8",
                ".json" => "application/json",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".gif" => "image/gif",
                ".ico" => "image/x-icon",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}
