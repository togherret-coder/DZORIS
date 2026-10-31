using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using KONTROLNIYA.Services;

namespace KONTROLNIYA
{
    [TestClass]
    public class MaterialServiceTests
    {
        [TestMethod]
        public void Create_ValidData_ReturnsDraftStatus()
        {
            var service = new MaterialService();
            var result = service.Create("Основы C#", "pdf");

            Assert.IsNotNull(result);
            Assert.AreEqual("draft", result.Status);
            Assert.AreEqual("Основы C#", result.Title);
            Assert.AreEqual(1, result.Id);
        }

        [TestMethod]
        public void Create_TitleTooShort_ThrowsArgumentException()
        {
            var service = new MaterialService();
            bool errorThrown = false;

            try
            {
                service.Create("C#", "video");
            }
            catch (ArgumentException)
            {
                errorThrown = true;
            }

            Assert.IsTrue(errorThrown, "Ожидалось исключение ArgumentException, но его не было.");
        }

        [TestMethod]
        public void GetFiltered_ByFormatAndStatus_ReturnsCorrectMaterials()
        {
            var service = new MaterialService();
            service.Create("Материал 1", "pdf");

            var mat2 = service.Create("Материал 2", "pdf");
            service.Update(mat2.Id, "Материал 2", "pdf", "published");

            service.Create("Материал 3", "video");

            var filtered = service.GetFiltered("pdf", "published");

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Материал 2", filtered[0].Title);
        }
    }
}