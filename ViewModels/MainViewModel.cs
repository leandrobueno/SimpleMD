using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using SimpleMD.Models;
using SimpleMD.Services;
using SimpleMD.Commands;
using SimpleMD.Helpers;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using System.Text.Json;

namespace SimpleMD.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IMarkdownService _markdownService;
        private readonly IThemeService _themeService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly RecentFilesManager _recentFilesManager;
        private readonly AppSettings _appSettings;
        
        private FileSystemWatcher? _fileWatcher;
        private string? _currentFilePath;
        private string? _currentMarkdownContent;
        private string _windowTitle = "SimpleMD - Markdown Viewer";
        private string _statusText = "No file loaded";
        private string _wordCountText = "0 words";
        private double _zoomLevel = 100;
        private bool _isLoading;
        private bool _hasDocument;
        private bool _isTocVisible;
        private double _tocWidth = 280;
        private ObservableCollection<TocItem> _tocItems;
        
        // Commands
        public ICommand OpenFileCommand { get; }
        public ICommand SaveAsHtmlCommand { get; }
        public ICommand SaveAsPdfCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ToggleTocCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowAboutCommand { get; }
        public ICommand NavigateToTocItemCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand OpenRecentFileCommand { get; }
        
        // Events
        public event EventHandler<string>? HtmlContentChanged;
        public event EventHandler<double>? ZoomLevelChanged;
        public event EventHandler<string>? NavigateToHeader;
        public event EventHandler<bool>? TocVisibilityChanged;
        public event EventHandler? PrintRequested;
        public event EventHandler<PdfExportSettings>? ExportPdfRequested;
        
        public MainViewModel(
            IMarkdownService markdownService, 
            IThemeService themeService,
            IFileService fileService,
            IDialogService dialogService)
        {
            _markdownService = markdownService;
            _themeService = themeService;
            _fileService = fileService;
            _dialogService = dialogService;
            _recentFilesManager = new RecentFilesManager();
            _appSettings = AppSettings.Instance;
            _tocItems = new ObservableCollection<TocItem>();
            
            // Initialize settings
            _zoomLevel = _appSettings.DefaultZoom;
            _isTocVisible = _appSettings.ShowTocByDefault;
            _tocWidth = _appSettings.TocWidth;
            
            // Initialize commands
            OpenFileCommand = new RelayCommand(async () => await OpenFileAsync());
            SaveAsHtmlCommand = new RelayCommand(async () => await SaveAsHtmlAsync(), () => HasDocument);
            SaveAsPdfCommand = new RelayCommand(async () => await SaveAsPdfAsync(), () => HasDocument);
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => HasDocument);
            ZoomInCommand = new RelayCommand(ZoomIn, () => HasDocument);
            ZoomOutCommand = new RelayCommand(ZoomOut, () => HasDocument);
            ToggleTocCommand = new RelayCommand(ToggleToc);
            PrintCommand = new RelayCommand(Print, () => HasDocument);
            ShowSettingsCommand = new RelayCommand(async () => await ShowSettingsAsync());
            ShowAboutCommand = new RelayCommand(async () => await ShowAboutAsync());
            NavigateToTocItemCommand = new RelayCommand<TocItem>(NavigateToTocItem);
            DropCommand = new RelayCommand<DragEventArgs>(async (e) => await HandleDropAsync(e));
            OpenRecentFileCommand = new RelayCommand<StorageFile>(async (file) => await LoadFileAsync(file?.Path));
            
            // Subscribe to theme changes
            _themeService.ThemeChanged += OnThemeChanged;
        }
        
        #region Properties
        
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (_currentFilePath != value)
                {
                    _currentFilePath = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string? CurrentMarkdownContent
        {
            get => _currentMarkdownContent;
            set
            {
                if (_currentMarkdownContent != value)
                {
                    _currentMarkdownContent = value;
                    OnPropertyChanged();
                    UpdateWordCount();
                }
            }
        }
        
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                if (_windowTitle != value)
                {
                    _windowTitle = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string WordCountText
        {
            get => _wordCountText;
            set
            {
                if (_wordCountText != value)
                {
                    _wordCountText = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                if (_zoomLevel != value)
                {
                    _zoomLevel = Math.Max(50, Math.Min(200, value));
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ZoomLevelText));
                    ZoomLevelChanged?.Invoke(this, _zoomLevel);
                }
            }
        }
        
        public string ZoomLevelText => $"{_zoomLevel}%";
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public bool HasDocument
        {
            get => _hasDocument;
            set
            {
                if (_hasDocument != value)
                {
                    _hasDocument = value;
                    OnPropertyChanged();
                    UpdateCommandStates();
                }
            }
        }
        
        public bool IsTocVisible
        {
            get => _isTocVisible;
            set
            {
                if (_isTocVisible != value)
                {
                    _isTocVisible = value;
                    OnPropertyChanged();
                    TocVisibilityChanged?.Invoke(this, _isTocVisible);
                }
            }
        }
        
        public double TocWidth
        {
            get => _tocWidth;
            set
            {
                if (_tocWidth != value)
                {
                    _tocWidth = Math.Max(200, Math.Min(500, value));
                    OnPropertyChanged();
                    _appSettings.TocWidth = _tocWidth;
                    _appSettings.Save();
                }
            }
        }
        
        public ObservableCollection<TocItem> TocItems
        {
            get => _tocItems;
            set
            {
                if (_tocItems != value)
                {
                    _tocItems = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public IReadOnlyList<RecentFile> RecentFiles => _recentFilesManager.RecentFiles;
        
        public AppSettings Settings => _appSettings;
        
        #endregion
        
        #region Commands Implementation
        
        private async Task OpenFileAsync()
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            var filePath = await _fileService.OpenFileAsync(hwnd);
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await LoadFileAsync(filePath);
            }
        }
        
        private async Task SaveAsHtmlAsync()
        {
            if (string.IsNullOrEmpty(CurrentMarkdownContent))
                return;
                
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            var suggestedName = _fileService.GetFileNameWithoutExtension(CurrentFilePath ?? "document") + ".html";
            
            var html = _markdownService.ConvertToHtml(CurrentMarkdownContent, _themeService.IsDarkMode);
            var savedPath = await _fileService.SaveFileAsync(hwnd, html, suggestedName);
            
            if (!string.IsNullOrEmpty(savedPath))
            {
                await _dialogService.ShowInfoAsync("Export Complete", $"HTML file saved to {savedPath}");
            }
        }
        
        private Task SaveAsPdfAsync()
        {
            if (!HasDocument)
                return Task.CompletedTask;
                
            // Raise event for PDF export - let the view handle the WebView2 interaction
            var settings = new PdfExportSettings
            {
                PageSize = PdfSettings.Load().PageSize,
                Orientation = PdfSettings.Load().Orientation,
                MarginTop = PdfSettings.Load().MarginTop,
                MarginBottom = PdfSettings.Load().MarginBottom,
                MarginLeft = PdfSettings.Load().MarginLeft,
                MarginRight = PdfSettings.Load().MarginRight,
                PrintBackgrounds = PdfSettings.Load().PrintBackgrounds,
                PrintHeaderFooter = PdfSettings.Load().PrintHeaderFooter,
                Scale = PdfSettings.Load().Scale
            };
            
            ExportPdfRequested?.Invoke(this, settings);
            return Task.CompletedTask;
        }
        
        private async Task RefreshAsync()
        {
            if (!string.IsNullOrEmpty(CurrentFilePath))
            {
                await LoadFileAsync(CurrentFilePath);
            }
        }
        
        private void ZoomIn()
        {
            ZoomLevel += 10;
        }
        
        private void ZoomOut()
        {
            ZoomLevel -= 10;
        }
        
        private void ToggleToc()
        {
            IsTocVisible = !IsTocVisible;
        }
        
        private void Print()
        {
            PrintRequested?.Invoke(this, EventArgs.Empty);
        }
        
        private Task ShowSettingsAsync()
        {
            // Settings will be handled by the view with binding to Settings property
            return Task.CompletedTask;
        }
        
        private Task ShowAboutAsync()
        {
            // About will be handled by the view
            return Task.CompletedTask;
        }
        
        private void NavigateToTocItem(TocItem? item)
        {
            if (item != null)
            {
                NavigateToHeader?.Invoke(this, item.Id);
            }
        }
        
        private async Task HandleDropAsync(DragEventArgs? e)
        {
            if (e?.DataView.Contains(StandardDataFormats.StorageItems) == true)
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0 && items[0] is StorageFile file)
                {
                    if (_fileService.IsMarkdownFile(file.Path))
                    {
                        await LoadFileAsync(file.Path);
                    }
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public async Task LoadFileAsync(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;
                
            try
            {
                IsLoading = true;
                
                // Read file
                var content = await _fileService.ReadFileAsync(filePath);
                CurrentMarkdownContent = content;
                CurrentFilePath = filePath;
                
                // Update recent files
                _recentFilesManager.AddFile(filePath);
                OnPropertyChanged(nameof(RecentFiles));
                
                // Update status
                var fileName = _fileService.GetFileName(filePath);
                StatusText = fileName;
                
                // Update window title
                var title = _markdownService.ExtractTitle(content);
                WindowTitle = !string.IsNullOrEmpty(title) 
                    ? $"{title} - SimpleMD" 
                    : $"{fileName} - SimpleMD";
                
                // Build TOC
                BuildTableOfContents(content);
                
                HasDocument = true;
                
                // Convert to HTML and notify
                var html = _markdownService.ConvertToHtml(content, _themeService.IsDarkMode);
                HtmlContentChanged?.Invoke(this, html);
                
                // Set up file watcher
                SetupFileWatcher(filePath);
            }
            catch (Exception ex)
            {
                StatusText = "Failed to load file";
                await _dialogService.ShowErrorAsync("Failed to load file", ex.Message, ex.ToString());
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public void SetInitialFile(string filePath)
        {
            CurrentFilePath = filePath;
        }
        
        public string GetCurrentHtml()
        {
            if (string.IsNullOrEmpty(CurrentMarkdownContent))
                return string.Empty;
                
            return _markdownService.ConvertToHtml(CurrentMarkdownContent, _themeService.IsDarkMode);
        }
        
        public void HandleWebMessage(string messageJson)
        {
            try
            {
                var message = JsonSerializer.Deserialize(messageJson, AppJsonContext.Default.WebMessage);
                if (message != null)
                {
                    switch (message.Type)
                    {
                        case "openExternal":
                            if (!string.IsNullOrEmpty(message.Url))
                            {
                                _ = Windows.System.Launcher.LaunchUriAsync(new Uri(message.Url));
                            }
                            break;
                            
                        case "taskToggle":
                            // TODO: Implement task list toggle
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebMessage error: {ex.Message}");
            }
        }
        
        public bool IsDropAllowed(DragEventArgs e)
        {
            return e.DataView.Contains(StandardDataFormats.StorageItems);
        }
        
        #endregion
        
        #region Private Methods
        
        private void UpdateWordCount()
        {
            if (string.IsNullOrEmpty(CurrentMarkdownContent))
            {
                WordCountText = "0 words";
            }
            else
            {
                var count = _markdownService.GetWordCount(CurrentMarkdownContent);
                WordCountText = $"{count} words";
            }
        }
        
        private void BuildTableOfContents(string markdownContent)
        {
            TocItems.Clear();
            
            var headers = _markdownService.ExtractHeaders(markdownContent);
            System.Diagnostics.Debug.WriteLine($"Found {headers.Count} headers");
            
            if (headers.Count == 0)
                return;
                
            var rootItems = new List<TocItem>();
            var stack = new Stack<TocItem>();
            
            foreach (var header in headers)
            {
                var newItem = new TocItem
                {
                    Title = header.text,
                    Level = header.level,
                    Id = header.id
                };
                
                while (stack.Count > 0 && stack.Peek().Level >= header.level)
                {
                    stack.Pop();
                }
                
                if (stack.Count == 0)
                {
                    rootItems.Add(newItem);
                }
                else
                {
                    stack.Peek().Children.Add(newItem);
                }
                
                stack.Push(newItem);
            }
            
            foreach (var item in rootItems)
            {
                TocItems.Add(item);
            }
            
            System.Diagnostics.Debug.WriteLine($"Added {TocItems.Count} root items to TOC");
        }
        
        private void SetupFileWatcher(string filePath)
        {
            _fileWatcher?.Dispose();
            
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileName(filePath);
                
                if (!string.IsNullOrEmpty(directory))
                {
                    _fileWatcher = new FileSystemWatcher(directory, fileName)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                    };
                    
                    _fileWatcher.Changed += OnFileChanged;
                    _fileWatcher.EnableRaisingEvents = true;
                    
                    System.Diagnostics.Debug.WriteLine($"File watcher enabled for: {filePath}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // This happens when running without runFullTrust capability
                System.Diagnostics.Debug.WriteLine($"File watcher not available (no runFullTrust): {ex.Message}");
                // App will still work, user just needs to manually refresh with F5
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File watcher error: {ex.Message}");
            }
        }
        
        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(100); // Debounce
            
            if (!string.IsNullOrEmpty(CurrentFilePath) && App.MainWindow?.DispatcherQueue != null)
            {
                await App.MainWindow.DispatcherQueue.EnqueueAsync(async () =>
                {
                    await LoadFileAsync(CurrentFilePath);
                });
            }
        }
        
        private void OnThemeChanged(object? sender, ElementTheme e)
        {
            if (!string.IsNullOrEmpty(CurrentMarkdownContent))
            {
                var html = _markdownService.ConvertToHtml(CurrentMarkdownContent, _themeService.IsDarkMode);
                HtmlContentChanged?.Invoke(this, html);
            }
        }
        
        private void UpdateCommandStates()
        {
            (SaveAsHtmlCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (SaveAsPdfCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ZoomInCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ZoomOutCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PrintCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        #endregion
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
            
            if (_themeService != null)
            {
                _themeService.ThemeChanged -= OnThemeChanged;
            }
            
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
    
    // Helper classes
    public class PdfExportSettings
    {
        public string PageSize { get; set; } = "Letter";
        public string Orientation { get; set; } = "Portrait";
        public double MarginTop { get; set; } = 0.5;
        public double MarginBottom { get; set; } = 0.5;
        public double MarginLeft { get; set; } = 0.5;
        public double MarginRight { get; set; } = 0.5;
        public bool PrintBackgrounds { get; set; } = true;
        public bool PrintHeaderFooter { get; set; } = false;
        public double Scale { get; set; } = 100;
    }
    
    public class WebMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("line")]
        public string? Line { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("checked")]
        public bool Checked { get; set; }
    }
}
