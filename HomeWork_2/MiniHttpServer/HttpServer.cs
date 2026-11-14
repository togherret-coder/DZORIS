
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new HttpListener();

        public int Port { get; set; } = 8080;
        public string Path { get; set; } = "connection";

        public async Task StartAsync()
        {
            string prefix =
                $"http://127.0.0.1:{Port}/{Path.Trim('/')}/";

            _listener.Prefixes.Add(prefix);
            _listener.Start();

            Console.WriteLine($"Сервер запущен: {prefix}");

            try
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context =
                        await _listener.GetContextAsync();

                    await ProcessRequestAsync(context);
                }
            }
            catch (HttpListenerException)
                when (!_listener.IsListening)
            {
                // Сервер был остановлен командой stop.
            }
            catch (ObjectDisposedException)
                when (!_listener.IsListening)
            {
                // Listener был закрыт.
            }
            finally
            {
                if (_listener.IsListening)
                    _listener.Stop();
            }

            Console.WriteLine("Сервер завершил работу.");
        }



        private async Task ProcessRequestAsync(
            HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            Console.WriteLine($"Запрос: {request.Url}");

            response.Headers["Server"] = "MiniHttpServer/1.0";

            try
            {
                // Получаем путь из URL без /connection/
                string urlPath = request.Url?.AbsolutePath ?? "/";

                string prefixPath = "/" + Path.Trim('/') + "/";

                if (!urlPath.StartsWith(
                    prefixPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 404;
                    return;
                }

                string relativePath = Uri.UnescapeDataString(
                    urlPath.Substring(prefixPath.Length));

                // Если запрошена папка, открываем index.html
                if (string.IsNullOrEmpty(relativePath) ||
                    relativePath.EndsWith('/'))
                {
                    relativePath += "index.html";
                }

                // Каталог со статическими файлами
                string root = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        AppContext.BaseDirectory,
                        ConfigurationManager.Instance.Settings
                            .PublicDirectoryPath));

                // Преобразуем URL-путь в путь к файлу
                string filePath = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        root,
                        relativePath.Replace(
                            '/', System.IO.Path.DirectorySeparatorChar)));

                // Запрещаем выход за пределы static
                string rootPrefix = System.IO.Path.TrimEndingDirectorySeparator(root)
                    + System.IO.Path.DirectorySeparatorChar;

                if (!filePath.StartsWith(
                    rootPrefix,
                    StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 403;
                    return;
                }

                if (!File.Exists(filePath))
                {
                    response.StatusCode = 404;
                    return;
                }

                // Читаем файл как байты
                byte[] buffer = await File.ReadAllBytesAsync(filePath);

                response.StatusCode = 200;
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength64 = buffer.Length;

                await response.OutputStream.WriteAsync(buffer);

                Console.WriteLine($"Отправлен файл: {relativePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
            finally
            {
                response.Close();
            }
        }



        public void Stop()
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                Console.WriteLine("Остановка сервера...");
            }
        }
    }
}
