using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers
{
    [Controller]
    public class TestController : BaseController
    {
        [Get]
        public IHttpResult Index()
        {
            return new HtmlResult(
                "<h1>Hello from controller!</h1>");
        }


        [Get]
        public IHttpResult Info()
        {
            return new JsonResult(new
            {
                Id = 1,
                Message = "Hello World!",
                Success = true
            });
        }


        [Get]
        public IHttpResult Hello()
        {
            return Html("Hello World!");
        }

        [Post]
        public IHttpResult Submit(string name)
        {
            return Html($"POST request processed. Hello, {name}!");
        }

        [Get]
        public IHttpResult Greet(string name)
        {
            return Html($"Hello, {name}!");
        }




        [Get]
        public IHttpResult BadRequest()
        {
            return Json(
            new
            {
                Error = "Bad Request",
                Message = "Тестовая ошибка 400"
            },
            400);
        }


        [Get]
        public IHttpResult RequestInfo()
        {
            return Json(new
            {
                method = Context.Request.HttpMethod,
                path = Context.Request.Url?.AbsolutePath,
                userAgent = Context.Request.UserAgent
            });
        }

    }
}
