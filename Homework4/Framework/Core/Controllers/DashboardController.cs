using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using CustomHttpServer.Models;

namespace CustomHttpServer.Core.Controllers
{
    /// <summary>
    /// Контроллер личного кабинета.
    ///
    /// Маршруты:
    ///   GET /dashboard             — готовая HTML-страница кабинета
    ///   GET /dashboard/profile     — профиль пользователя (JSON)
    ///   GET /dashboard/activities  — активности в виде ГОТОВОЙ HTML-разметки
    /// </summary>
    public class DashboardController
    {
        // ==== Демо-данные (в реальном проекте придут из БД / сервиса) ====
        private static readonly UserProfile DemoUser = new()
        {
            Id = 42,
            FullName = "Иван Иванов",
            Email = "ivan@student.local",
            Group = "ПИ-21-1",
            AvatarUrl = "/image/logo.png",
            RegisteredAt = new DateTime(2024, 9, 1, 9, 0, 0, DateTimeKind.Utc)
        };

        private static readonly List<UserActivity> DemoActivities = new()
        {
            new UserActivity
            {
                Id = 1,
                Type = "login",
                Title = "Вход в систему",
                Description = "Успешный вход с IP 127.0.0.1",
                Timestamp = DateTime.UtcNow.AddMinutes(-35)
            },
            new UserActivity
            {
                Id = 2,
                Type = "submit",
                Title = "Сдана лабораторная №3",
                Description = "Курс: «Web-программирование»",
                Timestamp = DateTime.UtcNow.AddHours(-5)
            },
            new UserActivity
            {
                Id = 3,
                Type = "grade",
                Title = "Получена оценка 5",
                Description = "Лабораторная №2 — «HTTP-сервер на C#»",
                Timestamp = DateTime.UtcNow.AddDays(-1)
            },
            new UserActivity
            {
                Id = 4,
                Type = "message",
                Title = "Новое сообщение от преподавателя",
                Description = "Не забудьте про дедлайн в пятницу.",
                Timestamp = DateTime.UtcNow.AddDays(-2)
            }
        };

        public bool CanHandle(string path) =>
            path.Equals("/dashboard", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/dashboard/", StringComparison.OrdinalIgnoreCase);

        public async Task HandleAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string path = request.Url!.LocalPath.TrimEnd('/');
            if (path.Length == 0) path = "/dashboard";

            if (!request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = 405;
                response.AddHeader("Allow", "GET");
                await WriteTextAsync(response, "text/plain; charset=utf-8",
                    "Method Not Allowed");
                return;
            }

            switch (path)
            {
                case "/dashboard":
                    await ServeDashboardPageAsync(response);
                    break;
                case "/dashboard/profile":
                    await ServeProfileJsonAsync(response);
                    break;
                case "/dashboard/activities":
                    await ServeActivitiesHtmlAsync(response);
                    break;
                default:
                    response.StatusCode = 404;
                    await WriteTextAsync(response, "text/html; charset=utf-8",
                        "<h1>404</h1><p>Раздел кабинета не найден.</p>");
                    break;
            }
        }

        // ---------- 1. Полная страница кабинета ----------
        private async Task ServeDashboardPageAsync(HttpListenerResponse response)
        {
            string activitiesHtml = RenderActivitiesHtml(DemoActivities);
            string profileHtml = RenderProfileHeader(DemoUser);

            string html = $@"<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Личный кабинет — {Html(DemoUser.FullName)}</title>
    <style>
        body {{ font-family: system-ui, sans-serif; background:#f4f6fb; margin:0; padding:40px; color:#1f2937; }}
        .wrap {{ max-width: 820px; margin: 0 auto; }}
        .card {{ background:#fff; border-radius:14px; padding:24px; box-shadow:0 4px 18px rgba(0,0,0,.06); margin-bottom:24px; }}
        .profile {{ display:flex; gap:20px; align-items:center; }}
        .avatar {{ width:72px; height:72px; border-radius:50%; background:#e5e7eb; object-fit:cover; }}
        .muted {{ color:#6b7280; font-size:14px; }}
        .activity-list {{ list-style:none; padding:0; margin:0; }}
        .activity {{ display:flex; gap:14px; padding:14px 0; border-bottom:1px solid #eef0f4; }}
        .activity:last-child {{ border-bottom:none; }}
        .activity__icon {{ font-size:22px; width:32px; text-align:center; }}
        .activity__title {{ font-weight:600; }}
        .activity__desc {{ color:#4b5563; font-size:14px; margin-top:2px; }}
        .activity__time {{ color:#9ca3af; font-size:12px; margin-top:4px; }}
    </style>
</head>
<body>
    <div class=""wrap"">
        <div class=""card"">
            {profileHtml}
        </div>
        <div class=""card"">
            <h2 style=""margin-top:0"">Последние активности</h2>
            <div id=""activities"">
                {activitiesHtml}
            </div>
        </div>
    </div>
</body>
</html>";
            await WriteTextAsync(response, "text/html; charset=utf-8", html);
        }

        // ---------- 2. Профиль (JSON) ----------
        private async Task ServeProfileJsonAsync(HttpListenerResponse response)
        {
            var json = JsonSerializer.Serialize(DemoUser, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await WriteTextAsync(response, "application/json; charset=utf-8", json);
        }

        // ---------- 3. Активности (готовая HTML-разметка) ----------
        private async Task ServeActivitiesHtmlAsync(HttpListenerResponse response)
        {
            string fragment = RenderActivitiesHtml(DemoActivities);
            await WriteTextAsync(response, "text/html; charset=utf-8", fragment);
        }

        // ================= РЕНДЕР =================

        private static string RenderProfileHeader(UserProfile u)
        {
            return $@"
<div class=""profile"">
    <img class=""avatar"" src=""{Html(u.AvatarUrl)}"" alt=""avatar"">
    <div>
        <h2 style=""margin:0"">{Html(u.FullName)}</h2>
        <div class=""muted"">{Html(u.Email)} · группа {Html(u.Group)}</div>
        <div class=""muted"">На платформе с {u.RegisteredAt:dd.MM.yyyy}</div>
    </div>
</div>";
        }

        /// <summary>
        /// Возвращает готовую HTML-разметку списка активностей.
        /// Никаких данных в открытом виде — только готовая вёрстка.
        /// </summary>
        private static string RenderActivitiesHtml(IEnumerable<UserActivity> activities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<ul class=\"activity-list\">");

            foreach (var a in activities)
            {
                string icon = a.Type switch
                {
                    "login" => "🔐",
                    "submit" => "📤",
                    "grade" => "🏆",
                    "message" => "✉️",
                    _ => "•"
                };

                sb.AppendLine($@"  <li class=""activity activity--{Html(a.Type)}"" data-id=""{a.Id}"">
    <span class=""activity__icon"">{icon}</span>
    <div class=""activity__body"">
      <div class=""activity__title"">{Html(a.Title)}</div>
      <div class=""activity__desc"">{Html(a.Description)}</div>
      <div class=""activity__time"" title=""{a.Timestamp:u}"">{a.Timestamp:dd.MM.yyyy HH:mm}</div>
    </div>
  </li>");
            }

            sb.AppendLine("</ul>");
            return sb.ToString();
        }

        // ================= ХЕЛПЕРЫ =================

        private static async Task WriteTextAsync(HttpListenerResponse response,
                                                 string contentType,
                                                 string body)
        {
            response.ContentType = contentType;
            byte[] buffer = Encoding.UTF8.GetBytes(body);
            response.ContentLength64 = buffer.Length;
            using Stream output = response.OutputStream;
            await output.WriteAsync(buffer, 0, buffer.Length);
        }

        private static string Html(string? s) =>
            HtmlEncoder.Default.Encode(s ?? string.Empty);
    }
}