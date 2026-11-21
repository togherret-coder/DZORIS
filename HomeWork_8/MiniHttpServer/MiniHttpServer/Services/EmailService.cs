
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MiniHttpServer.Framework.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniHttpServer.Services
{
    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(
            string email,
            string message)
        {
            // Получаем путь к файлу настроек
            string configPath = Path.Combine(
                AppContext.BaseDirectory,
                "smtpsettings.json");

            // Проверяем наличие файла
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(
                    "Файл smtpsettings.json не найден.");
            }

            // Читаем настройки
            string json = await File.ReadAllTextAsync(configPath);

            SmtpSettings settings =
                JsonSerializer.Deserialize<SmtpSettings>(json)
                ?? throw new Exception(
                    "Не удалось прочитать настройки SMTP.");

            // Проверяем обязательные поля
            if (string.IsNullOrWhiteSpace(settings.Username) ||
                string.IsNullOrWhiteSpace(settings.Password))
            {
                throw new Exception(
                    "Не указан логин или пароль SMTP.");
            }

            // Создаём письмо
            var mail = new MimeMessage();

            mail.From.Add(
                MailboxAddress.Parse(settings.Username));

            mail.To.Add(
                MailboxAddress.Parse(email));

            mail.Subject = "Сообщение от MiniHttpServer";

            mail.Body = new TextPart("plain")
            {
                Text = message
            };

            // Подключаемся к Mail.ru
            using var client = new SmtpClient();

            await client.ConnectAsync(
                settings.Host,
                settings.Port,
                SecureSocketOptions.SslOnConnect);

            // Авторизуемся
            await client.AuthenticateAsync(
                settings.Username,
                settings.Password);

            // Отправляем письмо
            await client.SendAsync(mail);

            // Отключаемся
            await client.DisconnectAsync(true);

            Console.WriteLine(
                $"Письмо успешно отправлено на {email}");
        }
    }
}
