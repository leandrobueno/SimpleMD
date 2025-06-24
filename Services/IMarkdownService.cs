using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleMD.Services
{
    public interface IMarkdownService
    {
        /// <summary>
        /// Converts markdown content to HTML with styling
        /// </summary>
        /// <param name="markdownContent">The markdown content to convert</param>
        /// <param name="isDarkMode">Whether to use dark mode styling</param>
        /// <param name="baseDirectory">Base directory for resolving relative image paths</param>
        /// <returns>Complete HTML document ready for display</returns>
        string ConvertToHtml(string markdownContent, bool isDarkMode = false, string? baseDirectory = null);

        /// <summary>
        /// Gets the word count from markdown content
        /// </summary>
        /// <param name="markdownContent">The markdown content</param>
        /// <returns>Number of words</returns>
        int GetWordCount(string markdownContent);

        /// <summary>
        /// Extracts the title from markdown content
        /// </summary>
        /// <param name="markdownContent">The markdown content</param>
        /// <returns>The title or null if not found</returns>
        string? ExtractTitle(string markdownContent);
        
        /// <summary>
        /// Extracts all headers from markdown content for table of contents
        /// </summary>
        /// <param name="markdownContent">The markdown content</param>
        /// <returns>List of headers with level and text</returns>
        List<(int level, string text, string id)> ExtractHeaders(string markdownContent);
    }
}
