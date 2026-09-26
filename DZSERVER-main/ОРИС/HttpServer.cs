using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ОРИС
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly Settings _settings;
        private bool _isRunning;

        public HttpServer(Settings settings)
        {
            _settings = settings;
            _listener = new HttpListener();

            // Формируем префикс для прослушки, например: http://127.0.0.1:8888/connection/
            string url = $"http://{_settings.Server.Host}:{_settings.Server.Port}/{_settings.Server.Path}/";
            _listener.Prefixes.Add(url);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"Сервер запущен. Перейдите по адресу: http://{_settings.Server.Host}:{_settings.Server.Port}/{_settings.Server.Path}/");
            Console.WriteLine("Введите 'stop' для остановки сервера.");

            // Цикл постоянного прослушивания запросов
            while (_isRunning)
            {
                try
                {
                    // Ожидаем входящий контекст асинхронно
                    var context = await _listener.GetContextAsync();

                    // Обрабатываем запрос в фоновой задаче, чтобы не блокировать цикл приема новых подключений
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // Возникает принудительно при вызове _listener.Stop()
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке запроса: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;

            string htmlPath = "search-engine.html";
            string responseText;

            if (File.Exists(htmlPath))
            {
                responseText = await File.ReadAllTextAsync(htmlPath, Encoding.UTF8);
            }
            else
            {
                responseText = "<h1>404 Файл search-engine.html не найден</h1>";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseText);

            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer);
            await output.FlushAsync();

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Запрос обработан: {context.Request.Url}");
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("Сервер остановлен.");
        }
    }
}