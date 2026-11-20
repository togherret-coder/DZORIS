
using MiniHttpServer.Framework.Attributes;

namespace MiniHttpServer.Controllers
{
    [Controller]
    public class TestController
    {
        [Get]
        public string Index()
        {
            return "Hello from controller!";
        }

        [Get]
        public string Hello()
        {
            return "Hello World!";
        }


        [Post]
        public string Submit(string name)
        {
            return $"POST request processed. Hello, {name}!";
        }

        [Get]
        public string Greet(string name)
        {
            return $"Hello, {name}!";
        }


    }
}
