using MiniHttpServer.HtmlTemplating;
using System;
using System.Net;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.Controllers
{
    public abstract class BaseController
    {
        protected HttpListenerContext Context { get; private set; } = null!;

        public void SetContext(HttpListenerContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }


        protected IHttpResult Html(string html, int statusCode = 200)
        {
            return new HtmlResult(html, statusCode);
        }

        protected IHttpResult Json(object data, int statusCode = 200)
        {
            return new JsonResult(data, statusCode);
        }


        protected IHttpResult Page(
            string templatePath,
            object dataModel)
        {
            IHtmlTemplateRenderer renderer =
                new HtmlTemplateRenderer();

            return new PageResult(
                templatePath,
                dataModel,
                renderer);
        }

    }
}
