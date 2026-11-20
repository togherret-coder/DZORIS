
using MiniHttpServer.Framework;
using MiniHttpServer.Framework.Configuration;
using MiniHttpServer;

ServerSettings settings =
    ConfigurationManager.Instance.Settings;

HttpServer server = new HttpServer
{
    Port = settings.Port,
    Path = settings.Path
};

Task serverTask = server.StartAsync();

Console.WriteLine("Введите stop для остановки сервера.");

while (true)
{
    string? command = Console.ReadLine();

    if (command?.Trim().Equals(
        "stop", StringComparison.OrdinalIgnoreCase) == true
        || command == null)
    {
        server.Stop();
        break;
    }

    Console.WriteLine("Неизвестная команда.");
}

await serverTask;

Console.WriteLine("Программа завершена.");
