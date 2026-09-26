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
            string settingsJson = File.ReadAllText("settings.json");
            Settings setting = JsonSerializer.Deserialize<Settings>(settingsJson);

            // Создаем экземпляр нашего сервера
            HttpServer server = new HttpServer(setting);

            // Запускаем сервер в фоновой задаче, чтобы консоль не блокировалась
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