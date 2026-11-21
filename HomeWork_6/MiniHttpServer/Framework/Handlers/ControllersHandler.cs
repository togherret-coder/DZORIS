using MiniHttpServer.Framework.Results;
using MiniHttpServer.Framework.Controllers;
using System.IO;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using MiniHttpServer.Framework.Attributes;
using MiniHttpServer.Framework.Configuration;

namespace MiniHttpServer.Framework.Handlers
{
    public class ControllersHandler : RequestHandler
    {
        public override async Task HandleAsync(
            HttpListenerContext context)
        {
            string path = context.Request.Url?.AbsolutePath ?? "/";

            string prefix = "/" +
                ConfigurationManager.Instance.Settings.Path.Trim('/') + "/";

            if (!path.StartsWith(
                prefix, StringComparison.OrdinalIgnoreCase))
            {
                await HandleNextAsync(context);
                return;
            }

            string route = path.Substring(prefix.Length).Trim('/');
            string[] parts = route.Split('/');

            if (parts.Length != 2)
            {
                await HandleNextAsync(context);
                return;
            }

            string controllerName = parts[0] + "Controller";
            string methodName = parts[1];
            string httpMethod = context.Request.HttpMethod;

            Assembly assembly = Assembly.GetExecutingAssembly();

            Type? controllerType = assembly.GetTypes()
                .FirstOrDefault(type =>
                    type.Name.Equals(
                        controllerName,
                        StringComparison.OrdinalIgnoreCase)
                    && type.IsDefined(
                        typeof(ControllerAttribute), false));

            if (controllerType == null)
            {
                await HandleNextAsync(context);
                return;
            }

            MethodInfo? method = controllerType.GetMethods()
                .FirstOrDefault(m =>
                    m.Name.Equals(
                        methodName,
                        StringComparison.OrdinalIgnoreCase)
                    && (
                        (httpMethod == "GET" &&
                         m.IsDefined(typeof(GetAttribute), false))
                        ||
                        (httpMethod == "POST" &&
                         m.IsDefined(typeof(PostAttribute), false))
                    ));

            if (method == null)
            {
                await HandleNextAsync(context);
                return;
            }

            object? controller = Activator.CreateInstance(controllerType);

            if (controller == null)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }

            if (controller is BaseController baseController)
            {
                baseController.SetContext(context);
            }

            ParameterInfo[] parameters = method.GetParameters();
            object?[] arguments = new object?[parameters.Length];

            NameValueCollection requestData;

            if (httpMethod == "POST")
            {
                using var reader = new StreamReader(
                    context.Request.InputStream,
                    context.Request.ContentEncoding);

                string body = await reader.ReadToEndAsync();

                requestData = System.Web.HttpUtility.ParseQueryString(body);
            }
            else
            {
                requestData = context.Request.QueryString;
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                string parameterName = parameters[i].Name!;

                arguments[i] = requestData[parameterName];
            }





            object? result;

            try
            {
                result = method.Invoke(controller, arguments);
            }
            catch (TargetInvocationException ex)
            {
                Console.WriteLine(
                    $"Ошибка контроллера: {ex.InnerException ?? ex}");

                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }




            if (result is Task task)
            {
                try
                {
                    await task;

                    PropertyInfo? resultProperty =
                        task.GetType().GetProperty("Result");

                    result = resultProperty?.GetValue(task);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Ошибка асинхронного метода: {ex}");

                    context.Response.StatusCode = 500;
                    context.Response.Close();
                    return;
                }
            }




            if (result is IHttpResult httpResponse)
            {
                try
                {
                    await httpResponse.ExecuteAsync(context.Response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Ошибка выполнения HTTP-результата: {ex}");

                    // Если заголовки ещё не отправлены,
                    // пытаемся установить статус ошибки.
                    try
                    {
                        context.Response.StatusCode = 500;
                    }
                    catch (InvalidOperationException)
                    {
                        // Заголовки уже отправлены.
                    }
                }
                finally
                {
                    context.Response.Close();
                }

                return;
            }




            Console.WriteLine(
                $"Неподдерживаемый тип результата: " +
                $"{result?.GetType().FullName ?? "null"}");

            context.Response.StatusCode = 500;
            context.Response.Close();
            return;

        }

        private async Task HandleNextAsync(
            HttpListenerContext context)
        {
            if (Next != null)
            {
                await Next.HandleAsync(context);
            }
            else
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
    }
}
