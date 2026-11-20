
using System;
using System.Net;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Handlers;

namespace MiniHttpServer.Framework
{
    public class HttpServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly RequestHandler _firstHandler;

        public int Port { get; set; } = 8080;
        public string Path { get; set; } = "connection";



        public HttpServer()
        {
            var loggingHandler = new LoggingHandler();
            var staticFilesHandler = new StaticFilesHandler();
            var controllersHandler = new ControllersHandler();
            var notFoundHandler = new NotFoundHandler();

            loggingHandler.SetNext(staticFilesHandler);
            staticFilesHandler.SetNext(controllersHandler);
            controllersHandler.SetNext(notFoundHandler);

            _firstHandler = loggingHandler;
        }



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

                    await _firstHandler.HandleAsync(context);
                }
            }
            catch (HttpListenerException)
                when (!_listener.IsListening)
            {
                // Сервер остановлен.
            }
            catch (ObjectDisposedException)
                when (!_listener.IsListening)
            {
                // Listener закрыт.
            }
            finally
            {
                if (_listener.IsListening)
                    _listener.Stop();
            }

            Console.WriteLine("Сервер завершил работу.");
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
