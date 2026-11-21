using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.Framework.Results
{
    public sealed class PageResult : IHttpResult
    {
        private readonly string _templatePath;
        private readonly object _dataModel;
        private readonly IHtmlTemplateRenderer _renderer;

        public PageResult(
            string templatePath,
            object dataModel,
            IHtmlTemplateRenderer renderer)
        {
            _templatePath = templatePath;
            _dataModel = dataModel;
            _renderer = renderer;
        }

        public async Task ExecuteAsync(
            HttpListenerResponse response,
            CancellationToken cancellationToken = default)
        {

            string fullPath = Path.Combine(
                AppContext.BaseDirectory,
                "Templates",
                _templatePath);

            string html = _renderer.RenderFromFile(
                fullPath,
                _dataModel);


            var htmlResult = new HtmlResult(html);

            await htmlResult.ExecuteAsync(
                response,
                cancellationToken);
        }
    }
}
