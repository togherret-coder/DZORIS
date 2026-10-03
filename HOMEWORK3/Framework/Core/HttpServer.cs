using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ОРИС
{
    public class HttpServer
    {
        private readonly Settings _settings;
        private TcpListener? _listener;
        private bool _isRunning;

        public HttpServer(Settings settings)
        {
            _settings = settings;
        }

        public async Task StartAsync()
        {
            var host = IPAddress.Parse(_settings.Server.Host);
            var port = int.Parse(_settings.Server.Port);

            _listener = new TcpListener(host, port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"Сервер запущен на http://{host}:{port}");
            Console.WriteLine("Доступные сайты:");
            Console.WriteLine($" - http://{host}:{port}/search-engine");
            Console.WriteLine($" - http://{host}:{port}/SatisFactory");
            Console.WriteLine($" - http://{host}:{port}/Steam");
            Console.WriteLine("Введите 'stop' для остановки сервера.\n");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                        Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            Console.WriteLine("Сервер остановлен.");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var buffer = new byte[8192];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;

                var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                var lines = request.Split('\n');
                if (lines.Length == 0) return;

                var requestLine = lines[0].Split(' ');
                if (requestLine.Length < 2) return;

                var path = requestLine[1];

                var queryIndex = path.IndexOf('?');
                if (queryIndex != -1)
                    path = path.Substring(0, queryIndex);

                path = Uri.UnescapeDataString(path);

                Console.WriteLine($"Запрос: {path}");

                bool redirect = false;
                string filePath = GetFilePath(path, out redirect);

                // Если нужен редирект — отправляем HTTP 301
                if (redirect)
                {
                    var redirectResponse = $"HTTP/1.1 301 Moved Permanently\r\n" +
                                           $"Location: {filePath}\r\n" +
                                           $"Connection: close\r\n\r\n";
                    var redirectBytes = Encoding.UTF8.GetBytes(redirectResponse);
                    await stream.WriteAsync(redirectBytes, 0, redirectBytes.Length);
                    return;
                }

                byte[] responseBytes;
                string contentType;
                int statusCode = 200;
                string statusText = "OK";

                if (File.Exists(filePath))
                {
                    responseBytes = await File.ReadAllBytesAsync(filePath);
                    contentType = GetContentType(filePath);
                    Console.WriteLine($"  -> Файл найден: {filePath} ({responseBytes.Length} байт)");
                }
                else
                {
                    Console.WriteLine($"  -> Файл НЕ найден: {filePath}");
                    string notFoundPath = Path.Combine("Static", "404.html");
                    if (File.Exists(notFoundPath))
                    {
                        responseBytes = await File.ReadAllBytesAsync(notFoundPath);
                        contentType = "text/html; charset=utf-8";
                    }
                    else
                    {
                        var errorText = "404 Not Found";
                        responseBytes = Encoding.UTF8.GetBytes(errorText);
                        contentType = "text/plain; charset=utf-8";
                    }
                    statusCode = 404;
                    statusText = "Not Found";
                }

                var response = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                               $"Content-Type: {contentType}\r\n" +
                               $"Content-Length: {responseBytes.Length}\r\n" +
                               $"Connection: close\r\n\r\n";
                var headerBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
        }

        private string GetFilePath(string urlPath, out bool redirect)
        {
            redirect = false;
            urlPath = urlPath.Replace('\\', '/');
            var staticRoot = Path.GetFullPath("Static");

            var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "/search-engine", "search-engine" },
                { "/SatisFactory", "SatisFactory" },
                { "/Steam", "Steam" }
            };

            foreach (var route in routes)
            {
                // Если запрошена папка без слэша — редирект на папку со слэшем
                if (urlPath.Equals(route.Key, StringComparison.OrdinalIgnoreCase))
                {
                    redirect = true;
                    return route.Key + "/";
                }

                if (urlPath.StartsWith(route.Key + "/", StringComparison.OrdinalIgnoreCase))
                {
                    string relativePath = urlPath.Substring(route.Key.Length);
                    if (string.IsNullOrEmpty(relativePath) || relativePath == "/")
                        relativePath = "/index.html";

                    var fullPath = Path.Combine(staticRoot, route.Value, relativePath.TrimStart('/'));
                    var fullFilePath = Path.GetFullPath(fullPath);

                    if (fullFilePath.StartsWith(staticRoot))
                    {
                        if (File.Exists(fullFilePath))
                            return fullFilePath;
                        if (Directory.Exists(fullFilePath))
                            return Path.Combine(fullFilePath, "index.html");
                    }
                }
            }

            // Поиск по имени файла во всех подпапках
            string fileName = Path.GetFileName(urlPath);
            if (!string.IsNullOrEmpty(fileName) && !urlPath.EndsWith("/"))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(staticRoot, "*", SearchOption.AllDirectories))
                    {
                        var candidate = Path.Combine(dir, fileName);
                        var fullCandidate = Path.GetFullPath(candidate);
                        if (fullCandidate.StartsWith(staticRoot) && File.Exists(fullCandidate))
                        {
                            var ext = Path.GetExtension(fileName).ToLowerInvariant();
                            var allowedExtensions = new HashSet<string>
                            {
                                ".css", ".js", ".png", ".jpg", ".jpeg", ".gif",
                                ".svg", ".ico", ".woff", ".woff2", ".ttf", ".eot",
                                ".map", ".json", ".xml", ".txt", ".html", ".htm"
                            };
                            if (allowedExtensions.Contains(ext))
                                return fullCandidate;
                        }
                    }
                }
                catch { }
            }

            var notFoundPath = Path.Combine(staticRoot, "404.html");
            return File.Exists(notFoundPath) ? notFoundPath : Path.Combine(staticRoot, "404.html");
        }

        private string GetContentType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".html" or ".htm" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".txt" => "text/plain; charset=utf-8",
                ".xml" => "application/xml; charset=utf-8",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                _ => "application/octet-stream"
            };
        }
    }
}