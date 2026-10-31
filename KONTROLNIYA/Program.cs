using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KONTROLNIYA;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var settingsJson = File.ReadAllText("settings.json");
            var setting = JsonSerializer.Deserialize<Settings>(settingsJson);


            var server = new HttpServer(setting);

            var serverTask = server.StartAsync();

            Console.WriteLine("Введите 'stop' для остановки сервера...");

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