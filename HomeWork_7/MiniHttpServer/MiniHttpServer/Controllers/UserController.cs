
using MiniHttpServer.Framework.Controllers;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Controllers
{
    [Controller]
    public class UserController : BaseController
    {
        [Get]
        public IHttpResult Profile()
        {


            var model = new
            {
                User = new
                {
                    Name = "<script>alert('test')</script>",
                    Age = 20,
                    Email = "alex@example.com",
                    HasAge = true,
                    IsActive = true
                },



                Friends = new[]
                {
                    new
                    {
                        Name = "Максим",
                        IsActive = true,
                        Hobbies = new[] { "Гитара", "Футбол" }
                    },
                    new
                    {
                        Name = "Анна",
                        IsActive = false,
                        Hobbies = new[] { "Рисование", "Музыка" }
                    }
                }

            };
            
            return Page("User/Profile.html", model);


        }
    }
}
