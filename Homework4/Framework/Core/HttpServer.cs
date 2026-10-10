using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CustomHttpServer.Core.Controllers; 
using ОРИС;

namespace CustomHttpServer.Core
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
                Console.WriteLine($"   Текущая директория: {Directory.GetCurrentDirectory()}");
                return;
            }

            _listener = new HttpListener();
            string prefix = $"http://{_settings.Server.Host}:{_settings.Server.Port}/";
            _listener.Prefixes.Add(prefix);

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"❌ Ошибка запуска сервера: {ex.Message}");
                return;
            }

            _isRunning = true;
            Console.WriteLine($"✅ Сервер запущен: {prefix}");
            Console.WriteLine($"✅ Статические файлы: {Path.GetFullPath(_staticFolderPath)}");
            Console.WriteLine("   Ожидание запросов...");

            while (_isRunning)
            {
                try
                {
                    Console.WriteLine("   Жду запрос...");
                    var context = await _listener.GetContextAsync();
                    Console.WriteLine($"   Получен запрос: {context.Request.HttpMethod} {context.Request.Url.LocalPath}");
                    _ = Task.Run(() => ProcessRequestAsync(context));
                }
                catch (HttpListenerException ex)
                {
                    Console.WriteLine($"   HttpListenerException: {ex.Message}");
                    break;
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine("   Listener закрыт");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Ошибка в цикле: {ex.Message}");
                }
            }

            Console.WriteLine("   Цикл обработки завершён");
        }

        public void Stop()
        {
            Console.WriteLine("    Остановка сервера...");
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            Console.WriteLine("✅ Сервер завершил свою работу ");
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
{
    var request = context.Request;
    var response = context.Response;

    // 1. Объявляем path и method ОДИН РАЗ здесь — до любых проверок
    string path = request.Url?.LocalPath ?? "/";
    string method = request.HttpMethod;

    // 2. Контроллер личного кабинета
    var dashboard = new DashboardController();
    if (dashboard.CanHandle(path))
    {
        await dashboard.HandleAsync(context);
        return;
    }

    try
    {
        // ❌ здесь больше НЕТ повторных объявлений path и method

        Console.WriteLine($"   Обработка: {method} {path}");

        // ==========================================
        // МАРШРУТ /login
        // ==========================================
        if (path == "/login")
        {
            if (method == "GET")
            {
                string loginFilePath = Path.Combine(_staticFolderPath, "login.html");
                await SendFileAsync(response, loginFilePath);
                Console.WriteLine("   ✅ GET /login → отправлен login.html");
                return;
            }
            else if (method == "POST")
            {
                await HandleLoginPost(request, response);
                return;
            }
        }

        // POST на /Login-Form/login.html (форма без action)
        if (path == "/Login-Form/login.html" && method == "POST")
        {
            await HandleLoginPost(request, response);
            return;
        }

        // Статические файлы
        if (path == "/") path = "/index.html";

        string relativePath = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        string staticFilePath = Path.GetFullPath(Path.Combine(_staticFolderPath, relativePath));

        string fullStaticPath = Path.GetFullPath(_staticFolderPath);
        if (!staticFilePath.StartsWith(fullStaticPath, StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 403;
            await SendErrorAsync(response, 403, "Доступ запрещен");
            return;
        }

        if (!File.Exists(staticFilePath))
        {
            response.StatusCode = 404;
            string notFoundPath = Path.Combine(_staticFolderPath, "404.html");

            if (File.Exists(notFoundPath))
            {
                staticFilePath = notFoundPath;
            }
            else
            {
                await SendErrorAsync(response, 404, "Файл не найден");
                return;
            }
        }

        response.ContentType = GetContentType(Path.GetExtension(staticFilePath));
        byte[] buffer = await File.ReadAllBytesAsync(staticFilePath);
        response.ContentLength64 = buffer.Length;

        using (Stream output = response.OutputStream)
        {
            await output.WriteAsync(buffer, 0, buffer.Length);
        }

        Console.WriteLine($"   ✅ Отправлен: {path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Ошибка обработки: {ex.Message}");
        try
        {
            response.StatusCode = 500;
            await SendErrorAsync(response, 500, "Внутренняя ошибка сервера");
        }
        catch (Exception inner)
        {
            // убираем warning «Empty general catch clause»
            Console.WriteLine($"   Ошибка при отправке 500: {inner.Message}");
        }
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

                    if (key == "login" || key == "email") login = value;
                    if (key == "password") password = value;
                }
            }

            Console.WriteLine($"👤 login: {login}");
            Console.WriteLine($" password: {password}");

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

        private async Task SendErrorAsync(HttpListenerResponse response, int statusCode, string message)
        {
            response.StatusCode = statusCode;
            response.ContentType = "text/html; charset=utf-8";
            string html = $"<html><body><h1>{statusCode} - {message}</h1></body></html>";
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
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".webp" => "image/webp",
                ".txt" => "text/plain; charset=utf-8",
                ".xml" => "application/xml; charset=utf-8",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}