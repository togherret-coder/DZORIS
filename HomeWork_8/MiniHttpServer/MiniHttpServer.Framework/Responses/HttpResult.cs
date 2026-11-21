
namespace MiniHttpServer.Framework.Responses
{
    public class HttpResult
    {
        public int StatusCode { get; set; }

        public string Content { get; set; } = "";

        public HttpResult(int statusCode, string content)
        {
            StatusCode = statusCode;
            Content = content;
        }
    }
}
