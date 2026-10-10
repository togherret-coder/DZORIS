using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CustomHttpServer.Core;

namespace ОРИС
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Читаем конфигурацию из settings.json
                var settingsJson = File.ReadAllText("settings.json");
                var setting = JsonSerializer.Deserialize<Settings>(settingsJson);
                
                // Создаём и запускаем сервер
                var server = new HttpServer(setting);
                
                // ВАЖНО: используем await чтобы дождаться запуска
                var serverTask = server.StartAsync();
                
                Console.WriteLine("Введите 'stop' для остановки сервера...");
                
                // Ожидаем команду 'stop' от пользователя в консоли
                while (true)
                {
                    string? command = Console.ReadLine();
                    if (command?.Trim().ToLower() == "stop")
                    {
                        server.Stop();
                        break;
                    }
                }
                
                // Дожидаемся завершения работы сервера
                await serverTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadLine();
            }
        }
    }
}