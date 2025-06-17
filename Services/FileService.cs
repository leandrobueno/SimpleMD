using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace SimpleMD.Services
{
    public interface IFileService
    {
        Task<string?> OpenFileAsync(IntPtr windowHandle);
        Task<string?> SaveFileAsync(IntPtr windowHandle, string content, string? suggestedFileName = null);
        Task<string> ReadFileAsync(string filePath);
        Task WriteFileAsync(string filePath, string content);
        bool IsMarkdownFile(string filePath);
        string GetFileNameWithoutExtension(string filePath);
        string GetFileName(string filePath);
        bool FileExists(string filePath);
    }
    
    public class FileService : IFileService
    {
        private static readonly HashSet<string> MarkdownExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".markdown", ".mkd", ".mdwn", ".mdown", ".mdtxt", ".mdtext"
        };
        
        public async Task<string?> OpenFileAsync(IntPtr windowHandle)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            
            // Add markdown extensions
            foreach (var ext in MarkdownExtensions)
            {
                picker.FileTypeFilter.Add(ext);
            }
            picker.FileTypeFilter.Add(".txt");
            
            InitializeWithWindow.Initialize(picker, windowHandle);
            
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
        
        public async Task<string?> SaveFileAsync(IntPtr windowHandle, string content, string? suggestedFileName = null)
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = suggestedFileName ?? "document.md"
            };
            
            savePicker.FileTypeChoices.Add("Markdown files", new List<string> { ".md" });
            savePicker.FileTypeChoices.Add("All files", new List<string> { "." });
            
            InitializeWithWindow.Initialize(savePicker, windowHandle);
            
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await FileIO.WriteTextAsync(file, content);
                return file.Path;
            }
            
            return null;
        }
        
        public async Task<string> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }
            
            try
            {
                return await File.ReadAllTextAsync(filePath);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Access denied to file: {filePath}");
            }
            catch (IOException ex)
            {
                throw new IOException($"Error reading file: {ex.Message}", ex);
            }
        }
        
        public async Task WriteFileAsync(string filePath, string content)
        {
            try
            {
                await File.WriteAllTextAsync(filePath, content);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"Access denied to file: {filePath}");
            }
            catch (IOException ex)
            {
                throw new IOException($"Error writing file: {ex.Message}", ex);
            }
        }
        
        public bool IsMarkdownFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return MarkdownExtensions.Contains(extension);
        }
        
        public string GetFileNameWithoutExtension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }
        
        public string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }
        
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
