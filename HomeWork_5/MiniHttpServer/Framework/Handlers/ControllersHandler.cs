using MiniHttpServer.Framework.Responses;
using System.IO;
using System.Collections.Specialized;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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




            object? result = method.Invoke(controller, arguments);

            
            if (result is Task task)
            {
                await task;

                
                PropertyInfo? resultProperty =
                    task.GetType().GetProperty("Result");

                result = resultProperty?.GetValue(task);
            }

            
            int statusCode = 200;
            string responseText;

           
            if (result is HttpResult httpResult)
            {
                statusCode = httpResult.StatusCode;
                responseText = httpResult.Content;
            }
            else
            {
                responseText = result?.ToString() ?? "";
            }


            byte[] buffer = Encoding.UTF8.GetBytes(responseText);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;

            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
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
