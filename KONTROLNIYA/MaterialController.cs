using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using KONTROLNIYA.Services; 

namespace KONTROLNIYA
{
    public class MaterialController
    {
        private readonly MaterialService _service = new();

        public bool CanHandle(string path)
        {
            return path.StartsWith("/materials", StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string path = request.Url?.LocalPath ?? "/";
            string method = request.HttpMethod;

            try
            {
                if (path == "/materials" && method == "GET")
                {
                    await HandleGetList(request, response);
                }
                else if (path == "/materials/details" && method == "GET")
                {
                    await HandleGetDetails(request, response);
                }
                else if (path == "/materials/create" && method == "POST")
                {
                    await HandlePostCreate(request, response);
                }
                else if (path == "/materials/update" && method == "PUT")
                {
                    await HandlePutUpdate(request, response);
                }
                else
                {
                    response.StatusCode = 404;
                    await WriteResponse(response, "404 Not Found", "text/plain");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                await WriteResponse(response, $"500 Internal Server Error: {ex.Message}", "text/plain");
            }
        }

        private async Task HandleGetList(HttpListenerRequest request, HttpListenerResponse response)
        {
            string? format = request.QueryString["format"];
            string? status = request.QueryString["status"]; 

            var materials = _service.GetFiltered(format, status);

            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'><title>Материалы</title></head><body>");
            html.Append("<h1>Каталог учебных материалов</h1>");

            html.Append("<h2>Добавить материал</h2>");
            html.Append("<form method='POST' action='/materials/create'>");
            html.Append("Название: <input type='text' name='title' required minlength='3' maxlength='100'><br>");
            html.Append("Формат: <select name='format'><option value='pdf'>PDF</option><option value='video'>Video</option><option value='article'>Article</option></select><br>");
            html.Append("<button type='submit'>Создать</button></form>");

            // Фильтры
            html.Append("<h2>Фильтрация</h2>");
            html.Append("<form method='GET' action='/materials'>");
            html.Append("Формат: <input type='text' name='format' value='" + WebUtility.HtmlEncode(format) + "'> ");
            html.Append("Статус: <input type='text' name='status' value='" + WebUtility.HtmlEncode(status) + "'> ");
            html.Append("<button type='submit'>Применить</button></form><hr>");

            html.Append("<h2>Список материалов</h2><ul>");
            foreach (var m in materials)
            {
                html.Append($"<li><a href='/materials/details?id={m.Id}'>{WebUtility.HtmlEncode(m.Title)}</a> ");
                html.Append($"| Формат: {m.Format} | Статус: {m.Status}</li>");
            }
            html.Append("</ul></body></html>");

            response.StatusCode = 200;
            await WriteResponse(response, html.ToString(), "text/html; charset=utf-8");
        }

        private async Task HandleGetDetails(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!int.TryParse(request.QueryString["id"], out int id))
            {
                response.StatusCode = 400;
                await WriteResponse(response, "400 Bad Request: Неверный ID", "text/plain");
                return;
            }

            var material = _service.GetById(id);
            if (material == null)
            {
                response.StatusCode = 404;
                await WriteResponse(response, "404 Not Found: Материал не найден", "text/plain");
                return;
            }

            var html = $@"
            <html><head><meta charset='utf-8'><title>Детали</title></head><body>
                <h1>Материал: {WebUtility.HtmlEncode(material.Title)}</h1>
                <p>ID: {material.Id}</p>
                <p>Формат: {material.Format}</p>
                <p>Статус: {material.Status}</p>
                <a href='/materials'>Назад к списку</a>
            </body></html>";

            response.StatusCode = 200;
            await WriteResponse(response, html, "text/html; charset=utf-8");
        }

        private async Task HandlePostCreate(HttpListenerRequest request, HttpListenerResponse response)
        {
            var formData = await ReadFormData(request);
            string title = formData.GetValueOrDefault("title", "");
            string format = formData.GetValueOrDefault("format", "");

            try
            {
                _service.Create(title, format);
                response.StatusCode = 303;
                response.RedirectLocation = "/materials";
            }
            catch (ArgumentException ex)
            {
                response.StatusCode = 400;
                await WriteResponse(response, $"400 Bad Request: {ex.Message}", "text/plain");
            }
        }

        private async Task HandlePutUpdate(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!int.TryParse(request.QueryString["id"], out int id))
            {
                response.StatusCode = 400;
                await WriteResponse(response, "400 Bad Request: Неверный ID", "text/plain");
                return;
            }

            var formData = await ReadFormData(request);
            string title = formData.GetValueOrDefault("title", "");
            string format = formData.GetValueOrDefault("format", "");
            string status = formData.GetValueOrDefault("status", "");

            try
            {
                _service.Update(id, title, format, status);
                // Успешный PUT - ответ 204 No Content
                response.StatusCode = 204;
                response.Close();
            }
            catch (KeyNotFoundException)
            {
                response.StatusCode = 404;
                await WriteResponse(response, "404 Not Found: Материал не найден", "text/plain");
            }
            catch (ArgumentException ex)
            {
                response.StatusCode = 400;
                await WriteResponse(response, $"400 Bad Request: {ex.Message}", "text/plain");
            }
        }

        private async Task<Dictionary<string, string>> ReadFormData(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            string body = await reader.ReadToEndAsync();
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(body)) return result;

            foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    string key = Uri.UnescapeDataString(parts[0]);
                    string value = Uri.UnescapeDataString(parts[1].Replace('+', ' '));
                    result[key] = value;
                }
            }
            return result;
        }

        private async Task WriteResponse(HttpListenerResponse response, string content, string contentType)
        {
            response.ContentType = contentType;
            byte[] buffer = Encoding.UTF8.GetBytes(content);
            response.ContentLength64 = buffer.Length;
            using (Stream output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}