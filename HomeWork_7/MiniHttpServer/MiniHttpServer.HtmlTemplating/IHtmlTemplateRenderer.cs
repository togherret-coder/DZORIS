
namespace MiniHttpServer.HtmlTemplating
{
    /// <summary>
    /// Определяет контракт HTML-шаблонизатора
    /// для формирования HTML на основе модели данных.
    /// </summary>
    public interface IHtmlTemplateRenderer
    {
        /// <summary>
        /// Обрабатывает HTML-шаблон из строки.
        /// </summary>
        /// <param name="htmlTemplate">
        /// Исходный HTML-шаблон.
        /// </param>
        /// <param name="dataModel">
        /// Объект с данными для подстановки.
        /// </param>
        /// <returns>Готовая HTML-разметка.</returns>
        string RenderFromString(
            string htmlTemplate,
            object dataModel);

        /// <summary>
        /// Загружает HTML-шаблон из файла и обрабатывает его.
        /// </summary>
        /// <param name="filePath">
        /// Путь к исходному HTML-файлу.
        /// </param>
        /// <param name="dataModel">
        /// Объект с данными для подстановки.
        /// </param>
        /// <returns>Готовая HTML-разметка.</returns>
        string RenderFromFile(
            string filePath,
            object dataModel);

        /// <summary>
        /// Обрабатывает HTML-шаблон и сохраняет результат в файл.
        /// </summary>
        /// <param name="inputFilePath">
        /// Путь к исходному шаблону.
        /// </param>
        /// <param name="outputFilePath">
        /// Путь для сохранения результата.
        /// </param>
        /// <param name="dataModel">
        /// Объект с данными для подстановки.
        /// </param>
        /// <returns>Путь к созданному файлу.</returns>
        string RenderToFile(
            string inputFilePath,
            string outputFilePath,
            object dataModel);
    }
}
