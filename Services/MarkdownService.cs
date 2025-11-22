using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleMD.Services
{
    public class MarkdownService : IMarkdownService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownService()
        {
            // Configure Markdig with all useful extensions
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()  // Includes tables, footnotes, figures, etc.
                .UseEmojiAndSmiley()     // :emoji: support
                .UseTaskLists()          // - [ ] checkbox support
                .UseDiagrams()           // Mermaid diagram support
                .UseAutoLinks()          // Automatic URL linking
                .UseGenericAttributes()  // {.class #id} support
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub) // Auto-generate IDs
                .Build();
        }

        public string ConvertToHtml(string markdownContent, bool isDarkMode = false, string? baseDirectory = null)
        {
            if (string.IsNullOrEmpty(markdownContent))
            {
                markdownContent = "# Welcome to SimpleMD\n\nOpen a markdown file to begin.";
            }

            // Convert markdown to HTML - AutoIdentifiers will add IDs
            var bodyHtml = Markdown.ToHtml(markdownContent, _pipeline);

            // Resolve relative image paths if base directory is provided
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                bodyHtml = ResolveImagePaths(bodyHtml, baseDirectory);
            }

            // Wrap in complete HTML document with styling
            var html = GetHtmlTemplate(bodyHtml, isDarkMode);

            return html;
        }

        public int GetWordCount(string markdownContent)
        {
            if (string.IsNullOrEmpty(markdownContent))
                return 0;

            // Remove Markdown syntax and count words
            var plainText = Markdown.ToPlainText(markdownContent, _pipeline);
            var words = Regex.Matches(plainText, @"\b\w+\b");
            return words.Count;
        }

        public string? ExtractTitle(string markdownContent)
        {
            if (string.IsNullOrEmpty(markdownContent))
                return null;

            var document = Markdown.Parse(markdownContent, _pipeline);
            
            // Look for the first heading
            var firstHeading = document.Descendants<HeadingBlock>().FirstOrDefault();
            if (firstHeading != null)
            {
                return ExtractTextFromInlines(firstHeading.Inline);
            }

            return null;
        }
        
        public List<(int level, string text, string id)> ExtractHeaders(string markdownContent)
        {
            var headers = new List<(int level, string text, string id)>();
            
            if (string.IsNullOrEmpty(markdownContent))
                return headers;

            var document = Markdown.Parse(markdownContent, _pipeline);
            
            foreach (var heading in document.Descendants<HeadingBlock>())
            {
                var text = ExtractTextFromInlines(heading.Inline);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Try to get the ID from heading attributes if available
                    string? id = null;
                    var attrs = heading.TryGetAttributes();
                    if (attrs != null && !string.IsNullOrEmpty(attrs.Id))
                    {
                        id = attrs.Id;
                    }
                    else
                    {
                        // Generate GitHub-style ID to match AutoIdentifiers
                        id = GenerateGitHubStyleId(text);
                    }
                    
                    headers.Add((heading.Level, text, id));
                }
            }

            return headers;
        }
        
        private string GenerateGitHubStyleId(string text)
        {
            // GitHub-style ID generation to match Markdig's AutoIdentifiers
            var id = text.ToLowerInvariant();
            
            // Replace spaces with hyphens
            id = Regex.Replace(id, @"\s+", "-");
            
            // Remove any character that is not a letter, number, hyphen, or underscore
            // This includes periods, ampersands, etc.
            id = Regex.Replace(id, @"[^a-z0-9\-_]", "");
            
            // Replace multiple hyphens with single hyphen
            id = Regex.Replace(id, @"-+", "-");
            
            // Trim hyphens from start and end
            id = id.Trim('-');
            
            return string.IsNullOrEmpty(id) ? "section" : id;
        }
        
        private string ExtractTextFromInlines(ContainerInline? inlines)
        {
            if (inlines == null)
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var inline in inlines)
            {
                ExtractTextFromInline(inline, sb);
            }
            return sb.ToString().Trim();
        }

        private void ExtractTextFromInline(Inline inline, StringBuilder sb)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    sb.Append(literal.Content);
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case EmphasisInline emphasis:
                    // Recursively extract text from emphasis content
                    foreach (var child in emphasis)
                    {
                        ExtractTextFromInline(child, sb);
                    }
                    break;
                case LinkInline link:
                    // Extract text from link content (not the URL)
                    foreach (var child in link)
                    {
                        ExtractTextFromInline(child, sb);
                    }
                    break;
                case LineBreakInline:
                    sb.Append(' ');
                    break;
                case HtmlInline:
                    // Skip HTML inline content
                    break;
                case HtmlEntityInline htmlEntity:
                    sb.Append(htmlEntity.Transcoded);
                    break;
                case AutolinkInline autolink:
                    sb.Append(autolink.Url);
                    break;
                default:
                    // For any other inline types, try to get their string representation
                    if (inline is ContainerInline container)
                    {
                        foreach (var child in container)
                        {
                            ExtractTextFromInline(child, sb);
                        }
                    }
                    break;
            }
        }


        private string ResolveImagePaths(string html, string baseDirectory)
        {
            // Use regex to find img tags with src attributes
            var imgRegex = new Regex(@"<img([^>]*?)src=[""']([^""']*?)[""']([^>]*?)>", RegexOptions.IgnoreCase);

            return imgRegex.Replace(html, match =>
            {
                var beforeSrc = match.Groups[1].Value;
                var srcValue = match.Groups[2].Value;
                var afterSrc = match.Groups[3].Value;

                // Only process relative paths (not absolute URLs or absolute file paths)
                if (!Uri.IsWellFormedUriString(srcValue, UriKind.Absolute) &&
                    !Path.IsPathRooted(srcValue))
                {
                    // Combine with base directory
                    var fullPath = Path.Combine(baseDirectory, srcValue);

                    // Normalize the path and convert to forward slashes for web compatibility
                    fullPath = Path.GetFullPath(fullPath).Replace('\\', '/');

                    // Use https://appassets.example/ virtual host that will be mapped to local files
                    // This is more reliable than custom schemes for WebView2
                    var relativePath = Path.GetRelativePath(baseDirectory, fullPath).Replace('\\', '/');
                    srcValue = $"https://appassets.example/{relativePath}";
                }

                return $"<img{beforeSrc}src=\"{srcValue}\"{afterSrc}>";
            });
        }

        private string GetHtmlTemplate(string bodyHtml, bool isDarkMode)
        {
            var theme = isDarkMode ? "dark" : "light";
            var template = $@"
<!DOCTYPE html>
<html data-theme='{theme}'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>SimpleMD</title>
    <style>
        {GetCssStyles()}
    </style>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/themes/prism-tomorrow.min.css'>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/prism.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-csharp.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-javascript.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-python.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-json.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-markdown.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-yaml.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-bash.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/prism/1.29.0/components/prism-powershell.min.js'></script>
    <script src='https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js'></script>
</head>
<body>
    <article class='markdown-body'>
        {bodyHtml}
    </article>
    <script>
        // Handle external links
        document.addEventListener('click', function(e) {{
            if (e.target.tagName === 'A' && e.target.href) {{
                if (!e.target.href.startsWith(window.location.origin)) {{
                    e.preventDefault();
                    window.chrome.webview.postMessage({{
                        type: 'openExternal',
                        url: e.target.href
                    }});
                }}
            }}
        }});

        // Syntax highlighting
        Prism.highlightAll();

        // Initialize Mermaid diagrams
        // Note: Each NavigateToString creates a fresh document, so we initialize on every load
        if (typeof mermaid !== 'undefined') {{
            mermaid.initialize({{
                startOnLoad: true,
                theme: '{theme}',
                securityLevel: 'loose',
                fontFamily: '-apple-system, BlinkMacSystemFont, ""Segoe UI"", ""Noto Sans"", Helvetica, Arial, sans-serif'
            }});
        }}

        // Handle checkbox clicks in task lists
        document.querySelectorAll('input[type=""checkbox""]').forEach(checkbox => {{
            checkbox.addEventListener('click', function(e) {{
                e.preventDefault();
                window.chrome.webview.postMessage({{
                    type: 'taskToggle',
                    line: e.target.dataset.line,
                    checked: e.target.checked
                }});
            }});
        }});
        
        // Function to scroll to header
        function scrollToHeader(headerId) {{
            const element = document.getElementById(headerId);
            if (element) {{
                element.scrollIntoView({{ behavior: 'smooth', block: 'start' }});
                // Highlight briefly
                element.style.backgroundColor = 'var(--color-accent-emphasis)';
                element.style.color = 'white';
                element.style.padding = '4px 8px';
                element.style.borderRadius = '4px';
                element.style.transition = 'all 0.3s ease';
                
                setTimeout(() => {{
                    element.style.backgroundColor = '';
                    element.style.color = '';
                    element.style.padding = '';
                    element.style.borderRadius = '';
                }}, 1500);
            }}
        }}
        
        // Listen for messages from the host
        window.chrome.webview.addEventListener('message', function(e) {{
            if (e.data.type === 'scrollToHeader') {{
                scrollToHeader(e.data.headerId);
            }}
        }});
    </script>
</body>
</html>";
            return template;
        }

        private string GetCssStyles()
        {
            return @"
/* Base styles */
:root {
    --color-canvas-default: #ffffff;
    --color-canvas-subtle: #f6f8fa;
    --color-fg-default: #24292f;
    --color-fg-muted: #57606a;
    --color-fg-subtle: #6e7781;
    --color-border-default: #d0d7de;
    --color-border-muted: #d8dee4;
    --color-accent-fg: #0969da;
    --color-accent-emphasis: #0969da;
    --color-danger-fg: #cf222e;
    --color-success-fg: #1a7f37;
    --color-attention-fg: #9a6700;
    --color-canvas-code: #f6f8fa;
    --color-code-fg: #0550ae;
}

[data-theme='dark'] {
    --color-canvas-default: #0d1117;
    --color-canvas-subtle: #161b22;
    --color-fg-default: #c9d1d9;
    --color-fg-muted: #8b949e;
    --color-fg-subtle: #6e7681;
    --color-border-default: #30363d;
    --color-border-muted: #21262d;
    --color-accent-fg: #58a6ff;
    --color-accent-emphasis: #1f6feb;
    --color-danger-fg: #f85149;
    --color-success-fg: #3fb950;
    --color-attention-fg: #d29922;
    --color-canvas-code: #161b22;
    --color-code-fg: #79c0ff;
}

* {
    box-sizing: border-box;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji';
    font-size: 16px;
    line-height: 1.5;
    color: var(--color-fg-default);
    background-color: var(--color-canvas-default);
    margin: 0;
    padding: 0;
    word-wrap: break-word;
}

.markdown-body {
    max-width: 900px;
    margin: 0 auto;
    padding: 32px;
}

/* Headings */
h1, h2, h3, h4, h5, h6 {
    margin-top: 24px;
    margin-bottom: 16px;
    font-weight: 600;
    line-height: 1.25;
}

h1 {
    font-size: 2em;
    border-bottom: 1px solid var(--color-border-muted);
    padding-bottom: .3em;
}

h2 {
    font-size: 1.5em;
    border-bottom: 1px solid var(--color-border-muted);
    padding-bottom: .3em;
}

h3 { font-size: 1.25em; }
h4 { font-size: 1em; }
h5 { font-size: .875em; }
h6 { 
    font-size: .85em;
    color: var(--color-fg-muted);
}

/* Links */
a {
    color: var(--color-accent-fg);
    text-decoration: none;
}

a:hover {
    text-decoration: underline;
}

/* Paragraphs and lists */
p, ul, ol, dl, table, pre, details {
    margin-top: 0;
    margin-bottom: 16px;
}

/* Lists */
ul, ol {
    padding-left: 2em;
}

li + li {
    margin-top: .25em;
}

/* Task lists */
.task-list-item {
    list-style-type: none;
}

.task-list-item input {
    margin: 0 .2em .25em -1.4em;
    vertical-align: middle;
}

/* Code */
code {
    background-color: var(--color-canvas-code);
    border-radius: 6px;
    font-size: 85%;
    margin: 0;
    padding: .2em .4em;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    color: var(--color-code-fg);
}

pre {
    background-color: var(--color-canvas-subtle);
    border-radius: 6px;
    font-size: 85%;
    line-height: 1.45;
    overflow: auto;
    padding: 16px;
}

pre code {
    background-color: transparent;
    border: 0;
    display: inline;
    line-height: inherit;
    margin: 0;
    overflow: visible;
    padding: 0;
    word-wrap: normal;
    color: inherit;
}

/* Blockquotes */
blockquote {
    border-left: .25em solid var(--color-border-default);
    color: var(--color-fg-muted);
    padding: 0 1em;
    margin: 0 0 16px 0;
}

blockquote > :first-child {
    margin-top: 0;
}

blockquote > :last-child {
    margin-bottom: 0;
}

/* Tables */
table {
    border-collapse: collapse;
    border-spacing: 0;
    width: 100%;
    overflow: auto;
}

table th {
    font-weight: 600;
}

table th, table td {
    border: 1px solid var(--color-border-default);
    padding: 6px 13px;
}

table tr {
    background-color: var(--color-canvas-default);
    border-top: 1px solid var(--color-border-muted);
}

table tr:nth-child(2n) {
    background-color: var(--color-canvas-subtle);
}

/* Horizontal rules */
hr {
    background-color: var(--color-border-default);
    border: 0;
    height: .25em;
    margin: 24px 0;
    padding: 0;
}

/* Images */
img {
    background-color: var(--color-canvas-default);
    box-sizing: content-box;
    max-width: 100%;
    height: auto;
    border-radius: 6px;
}

/* Broken image styling */
img[alt]:after {
    display: block;
    content: '🖼️ ' attr(alt);
    color: var(--color-fg-muted);
    background-color: var(--color-canvas-subtle);
    border: 1px dashed var(--color-border-default);
    border-radius: 6px;
    padding: 16px;
    text-align: center;
    font-style: italic;
}

/* Keyboard */
kbd {
    background-color: var(--color-canvas-subtle);
    border: 1px solid var(--color-border-default);
    border-radius: 6px;
    box-shadow: inset 0 -1px 0 var(--color-border-default);
    color: var(--color-fg-default);
    display: inline-block;
    font-size: 11px;
    line-height: 11px;
    padding: 4px 5px;
    vertical-align: middle;
}

/* Details/Summary */
details {
    border: 1px solid var(--color-border-default);
    border-radius: 6px;
    padding: .5em .5em 0;
}

summary {
    font-weight: 600;
    cursor: pointer;
    padding: .5em;
    margin: -.5em -.5em 0;
}

details[open] summary {
    margin-bottom: .5em;
}

/* Alerts/Admonitions */
.markdown-alert {
    border-left: .25em solid;
    margin-bottom: 16px;
    padding: .5em 1em;
}

.markdown-alert-note {
    border-color: var(--color-accent-emphasis);
    background-color: var(--color-accent-subtle);
}

.markdown-alert-warning {
    border-color: var(--color-attention-fg);
    background-color: var(--color-attention-subtle);
}

.markdown-alert-danger {
    border-color: var(--color-danger-fg);
    background-color: var(--color-danger-subtle);
}

/* Mermaid diagrams */
.mermaid {
    display: flex;
    justify-content: center;
    margin: 16px 0;
}

.mermaid svg {
    max-width: 100%;
    height: auto;
}

/* Print styles */
@media print {
    body {
        background-color: #fff;
        color: #000;
    }
    
    .markdown-body {
        max-width: 100%;
        padding: 0;
        margin: 0;
    }
    
    a {
        color: #000;
        text-decoration: underline;
    }
    
    pre, code {
        background-color: #f6f8fa;
        color: #000;
    }
    
    /* Avoid page breaks inside elements */
    h1, h2, h3, h4, h5, h6 {
        page-break-after: avoid;
    }
    
    pre, blockquote, table {
        page-break-inside: avoid;
    }
    
    /* Ensure images fit on page */
    img {
        max-width: 100% !important;
        page-break-inside: avoid;
    }

    /* Mermaid diagrams in print */
    .mermaid {
        page-break-inside: avoid;
    }

    .mermaid svg {
        max-width: 100% !important;
    }
}";
        }
    }
}
