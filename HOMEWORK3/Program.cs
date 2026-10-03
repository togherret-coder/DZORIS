using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ОРИС
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Читаем конфигурацию из settings.json
            var settingsJson = File.ReadAllText("settings.json");
            var setting = JsonSerializer.Deserialize<Settings>(settingsJson);
            
            // Создаём и запускаем сервер
            var server = new HttpServer(setting);
            var serverTask = server.StartAsync();

            // Ожидаем команду 'stop' от пользователя в консоли
            while (true)
            {
                string command = Console.ReadLine();
                if (command?.Trim().ToLower() == "stop")
                {
                    server.Stop();
                    break;
                }
            }

            // Дожидаемся завершения работы сервера
            await serverTask;
        }
    }
}