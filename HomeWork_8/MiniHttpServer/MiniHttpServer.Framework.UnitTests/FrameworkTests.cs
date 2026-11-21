
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Framework.Results;

namespace MiniHttpServer.Framework.UnitTests.Results
{
    [TestClass]
    public class FrameworkTests
    {
        [TestMethod]
        public void HtmlResult_CanBeCreated()
        {
            // Arrange
            string html = "<h1>Привет!</h1>";

            // Act
            var result = new HtmlResult(html);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType<IHttpResult>(result);
        }
    }
}
