using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KONTROLNIYA;


namespace KONTROLNIYA
{
    public class HttpServer
    {
        private readonly Settings _settings;
        private readonly string _staticFolderPath;
        private HttpListener? _listener;
        private bool _isRunning;

        public HttpServer(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _staticFolderPath = string.IsNullOrWhiteSpace(_settings.Server.Path)
                ? "Static"
                : _settings.Server.Path;
        }

        public async Task StartAsync()
        {
            if (!Directory.Exists(_staticFolderPath))
            {
                Console.WriteLine($"❌ Ошибка: папка '{_staticFolderPath}' не найдена!");
                return;
            }

            _listener = new HttpListener();
            string prefix = $"http://{_settings.Server.Host}:{_settings.Server.Port}/";
            _listener.Prefixes.Add(prefix);

            try { _listener.Start(); }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                return;
            }

            _isRunning = true;
            Console.WriteLine($"✅ Сервер запущен: {prefix}");
            Console.WriteLine($"✅ Статические файлы: {Path.GetFullPath(_staticFolderPath)}");

            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    Console.WriteLine($"   Получен запрос: {context.Request.HttpMethod} {context.Request.Url.LocalPath}");
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                catch (Exception) { break; }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string path = request.Url?.LocalPath ?? "/";
            string method = request.HttpMethod;

            Console.WriteLine($"🔍 DEBUG: Получен путь: '{path}'");

            var materialController = new MaterialController();
            if (materialController.CanHandle(path))
            {
                await materialController.HandleAsync(context);
                return;
            }

            try
            {
                // === 3. ОБРАБОТКА ВХОДА (LOGIN) ===
                if (path == "/login")
                {
                    if (method == "GET")
                    {
                        string loginFilePath = Path.Combine(_staticFolderPath, "login.html");
                        await SendFileAsync(response, loginFilePath);
                        return;
                    }
                    else if (method == "POST")
                    {
                        await HandleLoginPost(request, response);
                        return;
                    }
                }

                if (path == "/Login-Form/login.html" && method == "POST")
                {
                    await HandleLoginPost(request, response);
                    return;
                }

                // === 4. ОБРАБОТКА СТАТИКИ ===
                if (path == "/") path = "/index.html";
                string relativePath = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string staticFilePath = Path.GetFullPath(Path.Combine(_staticFolderPath, relativePath));

                if (!File.Exists(staticFilePath))
                {
                    response.StatusCode = 404;
                    await SendErrorAsync(response, 404, "Файл не найден");
                    return;
                }

                response.ContentType = GetContentType(Path.GetExtension(staticFilePath));
                byte[] buffer = await File.ReadAllBytesAsync(staticFilePath);
                response.ContentLength64 = buffer.Length;

                using (Stream output = response.OutputStream)
                {
                    await output.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Ошибка обработки: {ex.Message}");
                response.StatusCode = 500;
            }
        }

        private async Task SendFileAsync(HttpListenerResponse response, string filePath)
        {
            if (!File.Exists(filePath))
            {
                response.StatusCode = 404;
                await SendErrorAsync(response, 404, "Файл не найден");
                return;
            }

            response.StatusCode = 200;
            response.ContentType = GetContentType(Path.GetExtension(filePath));
            byte[] buffer = await File.ReadAllBytesAsync(filePath);
            response.ContentLength64 = buffer.Length;

            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        private async Task HandleLoginPost(HttpListenerRequest request, HttpListenerResponse response)
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();

            Console.WriteLine($"   📦 Тело запроса: {body}");

            string login = "";
            string password = "";

            foreach (var pair in body.Split('&'))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    string key = Uri.UnescapeDataString(parts[0]);
                    string value = Uri.UnescapeDataString(parts[1].Replace('+', ' '));

                    if (key == "login" || key == "email" || key == "username") login = value;
                    if (key == "password") password = value;
                }
            }

            Console.WriteLine($"👤 login: {login}");
            Console.WriteLine($"   password: {password}");

            response.StatusCode = 200;
            response.ContentType = "text/html; charset=utf-8";
            string html = "<html><body><h3>✅ Данные получены!</h3><a href='/login'>Вернуться</a></body></html>";
            byte[] responseBytes = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = responseBytes.Length;
            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(responseBytes, 0, responseBytes.Length);
            }
        }

        private async Task SendErrorAsync(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            response.ContentType = "text/html; charset=utf-8";
            string html = $"<html><body><h3>{statusCode} - {message}</h3></body></html>";
            byte[] errorBytes = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = errorBytes.Length;

            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".html" or ".htm" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
        }
    }
}