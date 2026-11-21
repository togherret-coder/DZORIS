
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MiniHttpServer.Framework.Results
{
    public interface IHttpResult
    {
        Task ExecuteAsync(
            HttpListenerResponse response,
            CancellationToken cancellationToken = default);
    }
}
