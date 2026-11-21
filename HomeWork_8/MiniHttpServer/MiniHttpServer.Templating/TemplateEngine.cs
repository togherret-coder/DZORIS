
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniHttpServer.Templating
{
    public class TemplateEngine
    {
        public string Render(string template, object model)
        {
            return Regex.Replace(
                template,
                @"\$\{(\w+)\}",
                match =>
                {
                    string propertyName =
                        match.Groups[1].Value;

                    PropertyInfo? property =
                        model.GetType().GetProperty(propertyName);

                    if (property == null)
                    {
                        return match.Value;
                    }

                    object? value = property.GetValue(model);

                    return value?.ToString() ?? "";
                });
        }
    }
}
