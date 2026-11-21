using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.HtmlTemplating;

namespace MiniHttpServer.HtmlTemplating.UnitTests.Renderers
{
    [TestClass]
    public class HtmlTemplateRendererTests
    {
        private readonly HtmlTemplateRenderer _renderer = new();

        [TestMethod]
        public void RenderFromString_ReplacesSimpleVariable()
        {
            // Arrange
            string template = "<h1>${Name}</h1>";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(
                template, model);

            // Assert
            Assert.AreEqual("<h1>Alex</h1>", result);
        }

        [TestMethod]
        public void RenderFromString_ReplacesNestedProperty()
        {
            // Arrange
            string template = "<p>${User.Name}</p>";
            var model = new
            {
                User = new { Name = "Максим" }
            };

            // Act
            string result = _renderer.RenderFromString(
                template, model);

            // Assert
            Assert.AreEqual("<p>Максим</p>", result);
        }

        [TestMethod]
        public void RenderFromString_EncodesHtml()
        {
            // Arrange
            string template = "<p>${Name}</p>";
            var model = new
            {
                Name = "<script>alert('test')</script>"
            };

            // Act
            string result = _renderer.RenderFromString(
                template, model);

            // Assert
            Assert.IsFalse(result.Contains("<script>"));
            StringAssert.Contains(result, "&lt;script&gt;");
        }

        [TestMethod]
        public void RenderFromString_HandlesMissingProperty()
        {
            // Arrange
            string template = "<p>${Unknown}</p>";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(
                template, model);

            // Assert
            Assert.AreEqual("<p></p>", result);
        }

        [TestMethod]
        public void RenderFromString_HandlesEmptyTemplate()
        {
            // Arrange
            string template = "";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(
                template, model);

            // Assert
            Assert.AreEqual("", result);
        }
    
    
[TestMethod]
        public void RenderFromString_IfTrue_RendersContent()
        {
            // Arrange
            string template = "$if(IsActive)\n<p>Активен</p>\n$endif\n";
            var model = new { IsActive = true };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<p>Активен</p>\n", result);
        }

        [TestMethod]
        public void RenderFromString_IfFalse_RendersElse()
        {
            // Arrange
            string template =
                "$if(IsActive)\n<p>Активен</p>\n" +
                "$else\n<p>Неактивен</p>\n$endif\n";

            var model = new { IsActive = false };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<p>Неактивен</p>\n", result);
        }

        [TestMethod]
        public void RenderFromString_Foreach_RendersAllItems()
        {
            // Arrange
            string template =
                "$foreach(var friend in Friends)\n" +
                "<p>${friend.Name}</p>\n" +
                "$endfor\n";

            var model = new
            {
                Friends = new[]
                {
            new { Name = "Максим" },
            new { Name = "Анна" }
        }
            };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(
                "<p>Максим</p>\n<p>Анна</p>\n",
                result);
        }

        [TestMethod]
        public void RenderFromString_EmptyCollection_RendersNothing()
        {
            // Arrange
            string template =
                "$foreach(var item in Items)\n" +
                "<p>${item}</p>\n" +
                "$endfor\n";

            var model = new { Items = Array.Empty<string>() };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_NestedForeach_RendersItems()
        {
            // Arrange
            string template =
                "$foreach(var friend in Friends)\n" +
                "<h2>${friend.Name}</h2>\n" +
                "$foreach(var hobby in friend.Hobbies)\n" +
                "<p>${hobby}</p>\n" +
                "$endfor\n" +
                "$endfor\n";

            var model = new
            {
                Friends = new[]
                {
            new
            {
                Name = "Максим",
                Hobbies = new[] { "Гитара", "Футбол" }
            }
        }
            };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(
                "<h2>Максим</h2>\n<p>Гитара</p>\n<p>Футбол</p>\n",
                result);
        }

        [TestMethod]
        public void RenderFromString_IfInsideForeach_UsesItemProperty()
        {
            // Arrange
            string template =
                "$foreach(var friend in Friends)\n" +
                "$if(friend.IsActive)\n" +
                "<p>${friend.Name}: активен</p>\n" +
                "$else\n" +
                "<p>${friend.Name}: неактивен</p>\n" +
                "$endif\n" +
                "$endfor\n";

            var model = new
            {
                Friends = new[]
                {
            new { Name = "Максим", IsActive = true },
            new { Name = "Анна", IsActive = false }
        }
            };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(
                "<p>Максим: активен</p>\n" +
                "<p>Анна: неактивен</p>\n",
                result);
        }

        [TestMethod]
        public void RenderFromString_UnclosedIf_ThrowsException()
        {
            // Arrange
            string template = "$if(IsActive)\n<p>Тест</p>\n";
            var model = new { IsActive = true };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                _renderer.RenderFromString(template, model));
        }


        [TestMethod]
        public void RenderFromFile_ReadsAndRendersTemplate()
        {
            // Arrange
            string path = Path.GetTempFileName();

            try
            {
                File.WriteAllText(path, "<h1>${Name}</h1>");
                var model = new { Name = "Alex" };

                // Act
                string result = _renderer.RenderFromFile(path, model);

                // Assert
                Assert.AreEqual("<h1>Alex</h1>", result);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void RenderToFile_WritesRenderedHtml()
        {
            // Arrange
            string inputPath = Path.GetTempFileName();
            string outputPath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(inputPath, "<p>${Name}</p>");
                var model = new { Name = "Анна" };

                // Act
                string result = _renderer.RenderToFile(
                    inputPath, outputPath, model);

                // Assert
                Assert.AreEqual("<p>Анна</p>", result);
                Assert.AreEqual(
                    "<p>Анна</p>",
                    File.ReadAllText(outputPath));
            }
            finally
            {
                File.Delete(inputPath);
                File.Delete(outputPath);
            }
        }

        [TestMethod]
        public void RenderFromFile_MissingFile_ThrowsException()
        {
            // Arrange
            string path = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("N") + ".html");

            var model = new { Name = "Alex" };

            // Act & Assert
            Assert.ThrowsException<FileNotFoundException>(() =>
                _renderer.RenderFromFile(path, model));
        }


        [TestMethod]
        public void RenderFromString_ReplacesMultipleProperties()
        {
            // Arrange
            string template = "<p>${Name}, ${Age}</p>";
            var model = new { Name = "Alex", Age = 20 };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<p>Alex, 20</p>", result);
        }

        [TestMethod]
        public void RenderFromString_ReplacesRepeatedProperty()
        {
            // Arrange
            string template = "<p>${Name} и снова ${Name}</p>";
            var model = new { Name = "Alex" };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("<p>Alex и снова Alex</p>", result);
        }

        [TestMethod]
        public void RenderFromString_IfFalseWithoutElse_RendersNothing()
        {
            // Arrange
            string template = "$if(IsActive)\n<p>Активен</p>\n$endif\n";
            var model = new { IsActive = false };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void RenderFromString_NestedIf_RendersCorrectBranch()
        {
            // Arrange
            string template =
                "$if(IsActive)\n" +
                "<p>Активен</p>\n" +
                "$if(IsAdmin)\n" +
                "<p>Администратор</p>\n" +
                "$else\n" +
                "<p>Обычный пользователь</p>\n" +
                "$endif\n" +
                "$endif\n";

            var model = new
            {
                IsActive = true,
                IsAdmin = false
            };

            // Act
            string result = _renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(
                "<p>Активен</p>\n" +
                "<p>Обычный пользователь</p>\n",
                result);
        }

        [TestMethod]
        public void RenderFromString_UnclosedForeach_ThrowsException()
        {
            // Arrange
            string template =
                "$foreach(var item in Items)\n" +
                "<p>${item}</p>\n";

            var model = new { Items = new[] { "A", "B" } };

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                _renderer.RenderFromString(template, model));
        }

    }

}
