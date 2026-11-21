
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniHttpServer.HtmlTemplating
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        private static readonly Regex DirectivePattern = new(
            @"(?m)^[ \t]*\$(?:if\(([\w.]+)\)|(else)|(endif)|foreach\(var (\w+) in ([\w.]+)\)|(endfor))[ \t]*(?:\r?\n|$)");

        public string RenderFromString(
            string htmlTemplate,
            object dataModel)
        {
            return RenderBlock(
                htmlTemplate,
                dataModel,
                new Dictionary<string, object?>());
        }

        public string RenderFromFile(
            string filePath,
            object dataModel)
        {
            string template = File.ReadAllText(filePath);
            return RenderFromString(template, dataModel);
        }

        public string RenderToFile(
            string inputFilePath,
            string outputFilePath,
            object dataModel)
        {
            string result = RenderFromFile(
                inputFilePath, dataModel);

            File.WriteAllText(outputFilePath, result);
            return result;
        }

        private object? ResolveValue(
            string expression,
            object model,
            Dictionary<string, object?> variables)
        {
            string[] parts = expression.Split('.');
            object? value;
            int index;

            if (variables.TryGetValue(parts[0], out value))
            {
                index = 1;
            }
            else
            {
                value = model;
                index = 0;
            }

            for (int i = index; i < parts.Length; i++)
            {
                if (value == null)
                    return null;

                var property = value.GetType()
                    .GetProperty(parts[i]);

                if (property == null)
                    return null;

                value = property.GetValue(value);
            }

            return value;
        }

        private string ReplaceVariables(
            string template,
            object model,
            Dictionary<string, object?> variables)
        {
            return Regex.Replace(
                template,
                @"\$\{([\w.]+)\}",
                match =>
                {
                    object? value = ResolveValue(
                        match.Groups[1].Value,
                        model,
                        variables);

                    return WebUtility.HtmlEncode(
                        value?.ToString() ?? "");
                });
        }

        private string RenderBlock(
            string template,
            object model,
            Dictionary<string, object?> variables)
        {
            var matches = DirectivePattern.Matches(template);
            var result = new StringBuilder();

            int position = 0;

            for (int i = 0; i < matches.Count; i++)
            {
                Match start = matches[i];

                result.Append(ReplaceVariables(
                    template.Substring(
                        position,
                        start.Index - position),
                    model,
                    variables));

                bool isIf = start.Groups[1].Success;
                bool isForeach = start.Groups[4].Success;

                if (!isIf && !isForeach)
                    throw new InvalidOperationException(
                        "Неожиданная директива шаблона");

                var stack = new Stack<string>();
                stack.Push(isIf ? "if" : "foreach");

                int elseIndex = -1;
                int endIndex = -1;

                for (int j = i + 1; j < matches.Count; j++)
                {
                    Match current = matches[j];

                    if (current.Groups[1].Success)
                        stack.Push("if");
                    else if (current.Groups[4].Success)
                        stack.Push("foreach");
                    else if (current.Groups[2].Success)
                    {
                        if (stack.Peek() != "if")
                            throw new InvalidOperationException(
                                "$else без соответствующего $if");

                        if (stack.Count == 1)
                        {
                            if (elseIndex != -1)
                                throw new InvalidOperationException(
                                    "Повторный $else");

                            elseIndex = j;
                        }
                    }
                    else
                    {
                        string expected =
                            current.Groups[3].Success
                                ? "if"
                                : "foreach";

                        if (stack.Pop() != expected)
                            throw new InvalidOperationException(
                                "Неправильный порядок директив");

                        if (stack.Count == 0)
                        {
                            endIndex = j;
                            break;
                        }
                    }
                }

                if (endIndex == -1)
                    throw new InvalidOperationException(
                        "Не найден конец блока");

                Match end = matches[endIndex];
                int bodyStart = start.Index + start.Length;

                if (isIf)
                {
                    object? value = ResolveValue(
                        start.Groups[1].Value,
                        model,
                        variables);

                    bool condition = value is bool b && b;

                    int trueEnd = elseIndex >= 0
                        ? matches[elseIndex].Index
                        : end.Index;

                    string trueBody = template.Substring(
                        bodyStart,
                        trueEnd - bodyStart);

                    string falseBody = "";

                    if (elseIndex >= 0)
                    {
                        Match elseMatch = matches[elseIndex];

                        int falseStart =
                            elseMatch.Index + elseMatch.Length;

                        falseBody = template.Substring(
                            falseStart,
                            end.Index - falseStart);
                    }

                    result.Append(RenderBlock(
                        condition ? trueBody : falseBody,
                        model,
                        variables));
                }
                else
                {
                    object? collection = ResolveValue(
                        start.Groups[5].Value,
                        model,
                        variables);

                    string body = template.Substring(
                        bodyStart,
                        end.Index - bodyStart);

                    if (collection is IEnumerable items)
                    {
                        foreach (object? item in items)
                        {
                            var childVariables =
                                new Dictionary<string, object?>(
                                    variables);

                            childVariables[
                                start.Groups[4].Value] = item;

                            result.Append(RenderBlock(
                                body,
                                model,
                                childVariables));
                        }
                    }
                }

                position = end.Index + end.Length;
                i = endIndex;
            }

            result.Append(ReplaceVariables(
                template.Substring(position),
                model,
                variables));

            return result.ToString();
        }
    }
}
