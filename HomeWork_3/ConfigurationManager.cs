
using System;
using System.IO;
using System.Text.Json;

namespace MiniHttpServer
{
    public sealed class ConfigurationManager
    {
        private static readonly Lazy<ConfigurationManager>
            _instance = new(() => new ConfigurationManager());

        public static ConfigurationManager Instance
            => _instance.Value;

        public ServerSettings Settings { get; }

        private ConfigurationManager()
        {
            string filePath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "settings.json"
            );

            string json = File.ReadAllText(filePath);

            Settings =
                JsonSerializer.Deserialize<ServerSettings>(json)
                ?? throw new InvalidOperationException(
                    "Не удалось загрузить настройки сервера.");
        }
    }
}
