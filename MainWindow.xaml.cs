using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using SimpleMD.Services;
using SimpleMD.Controls;
using Windows.System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI.Core;
using SimpleMD.Helpers;

namespace SimpleMD
{
    public sealed partial class MainWindow : Window
    {
        private string? _currentFilePath;
        private string? _currentMarkdownContent;
        private double _currentZoom = 100;
        private readonly IMarkdownService _markdownService;
        private readonly IThemeService _themeService;
        private FileSystemWatcher? _fileWatcher;
        private ObservableCollection<TocItem> _tocItems = new ObservableCollection<TocItem>();

        public MainWindow()
        {
            this.InitializeComponent();

            // Initialize services
            _markdownService = new MarkdownService();
            _themeService = new ThemeService();
            _themeService.Initialize(this);
            _themeService.ThemeChanged += OnThemeChanged;

            // Set up the custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Initialize event handlers
            InitializeEventHandlers();

            // Set up drag and drop
            SetupDragAndDrop();
            
            // Initialize WebView
            _ = InitializeWebView();
        }

        public void SetInitialFile(string filePath)
        {
            // Load the file after the window is fully loaded
            _currentFilePath = filePath;
            var rootGrid = Content as Grid;
            if (rootGrid != null)
            {
                rootGrid.Loaded += async (s, e) =>
                {
                    await LoadMarkdownFile(filePath);
                };
            }
        }

        private void InitializeEventHandlers()
        {
            // File operations
            OpenFileButton.Click += OpenFileButton_Click;
            OpenFileButtonLarge.Click += OpenFileButton_Click;
            RefreshButton.Click += RefreshButton_Click;
            
            // Zoom controls
            ZoomInButton.Click += ZoomInButton_Click;
            ZoomOutButton.Click += ZoomOutButton_Click;
            
            // Secondary commands
            PrintButton.Click += PrintButton_Click;
            SaveAsHtmlButton.Click += SaveAsHtmlButton_Click;
            SaveAsPdfButton.Click += SaveAsPdfButton_Click;
            SettingsButton.Click += SettingsButton_Click;
            AboutButton.Click += AboutButton_Click;
            
            // WebView navigation
            MarkdownWebView.NavigationStarting += MarkdownWebView_NavigationStarting;
        }

        private void SetupDragAndDrop()
        {
            // Set up drag and drop for the main grid
            var mainGrid = Content as Grid;
            if (mainGrid != null)
            {
                mainGrid.AllowDrop = true;
                mainGrid.DragOver += OnDragOver;
                mainGrid.Drop += OnDrop;
                mainGrid.DragEnter += OnDragEnter;
                mainGrid.DragLeave += OnDragLeave;
            }
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add(".markdown");
            picker.FileTypeFilter.Add(".txt");

            // Initialize with the current window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadMarkdownFile(file.Path);
            }
        }

        public async Task LoadMarkdownFile(string filePath)
        {
            try
            {
                ShowLoading(true);

                // Read file content
                _currentMarkdownContent = await File.ReadAllTextAsync(filePath);
                _currentFilePath = filePath;

                // Convert to HTML
                var html = _markdownService.ConvertToHtml(_currentMarkdownContent, _themeService.IsDarkMode);

                // Update UI
                FilePathText.Text = System.IO.Path.GetFileName(filePath);
                ToolTipService.SetToolTip(FilePathText, filePath);
                
                // Update word count
                var wordCount = _markdownService.GetWordCount(_currentMarkdownContent);
                WordCountText.Text = $"{wordCount} words";
                
                // Update window title
                var title = _markdownService.ExtractTitle(_currentMarkdownContent);
                if (!string.IsNullOrEmpty(title))
                {
                    Title = $"{title} - SimpleMD";
                }
                else
                {
                    Title = $"{System.IO.Path.GetFileName(filePath)} - SimpleMD";
                }

                // Build and populate TOC
                BuildTableOfContents(_currentMarkdownContent);

                // Show WebView, hide welcome panel
                WelcomePanel.Visibility = Visibility.Collapsed;
                WebViewBorder.Visibility = Visibility.Visible;

                // Load HTML into WebView
                await MarkdownWebView.EnsureCoreWebView2Async();
                MarkdownWebView.NavigateToString(html);
                
                // Set up file watcher
                SetupFileWatcher(filePath);

                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                await ShowErrorDialog("Failed to load file", ex.Message);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                _ = LoadMarkdownFile(_currentFilePath);
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Min(_currentZoom + 10, 200);
            UpdateZoom();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            _currentZoom = Math.Max(_currentZoom - 10, 50);
            UpdateZoom();
        }

        private void UpdateZoom()
        {
            ZoomLevelText.Text = $"{_currentZoom}%";
            
            // Apply zoom to WebView
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                MarkdownWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                _ = MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.zoom = '{_currentZoom}%'");
            }
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (MarkdownWebView.CoreWebView2 != null)
            {
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync("window.print();");
            }
        }

        private async void SaveAsHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentMarkdownContent))
            {
                await ShowInfoDialog("No file loaded", "Please open a markdown file first.");
                return;
            }
            
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(_currentFilePath ?? "document") + ".html"
            };
            savePicker.FileTypeChoices.Add("HTML files", new List<string>() { ".html" });
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                var html = _markdownService.ConvertToHtml(_currentMarkdownContent, _themeService.IsDarkMode);
                await FileIO.WriteTextAsync(file, html);
                await ShowInfoDialog("Export Complete", $"HTML file saved to {file.Path}");
            }
        }

        private async void SaveAsPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentMarkdownContent))
            {
                await ShowInfoDialog("No file loaded", "Please open a markdown file first.");
                return;
            }

            if (MarkdownWebView?.CoreWebView2 == null)
            {
                await ShowErrorDialog("Export Error", "WebView is not ready. Please try again.");
                return;
            }

            // Load saved PDF settings
            var savedSettings = PdfSettings.Load();
            
            // Apply saved settings to dialog controls
            PageSizeComboBox.SelectedIndex = savedSettings.PageSize switch
            {
                "A4" => 1,
                "Legal" => 2,
                "A3" => 3,
                _ => 0 // Letter
            };
            
            OrientationComboBox.SelectedIndex = savedSettings.Orientation == "Landscape" ? 1 : 0;
            
            // Set unit toggle and convert margin values if needed
            MarginUnitToggle.IsOn = savedSettings.UseMetricUnits;
            if (savedSettings.UseMetricUnits)
            {
                // Convert stored inches to cm for display
                MarginTopBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginTop), 2);
                MarginBottomBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginBottom), 2);
                MarginLeftBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginLeft), 2);
                MarginRightBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginRight), 2);
                
                // Update NumberBox properties for metric
                UpdateMarginBoxesForMetric(true);
            }
            else
            {
                MarginTopBox.Value = savedSettings.MarginTop;
                MarginBottomBox.Value = savedSettings.MarginBottom;
                MarginLeftBox.Value = savedSettings.MarginLeft;
                MarginRightBox.Value = savedSettings.MarginRight;
                
                // Update NumberBox properties for imperial
                UpdateMarginBoxesForMetric(false);
            }
            
            PrintBackgroundsCheckBox.IsChecked = savedSettings.PrintBackgrounds;
            PrintHeaderFooterCheckBox.IsChecked = savedSettings.PrintHeaderFooter;
            ScaleSlider.Value = savedSettings.Scale;

            // Show PDF settings dialog
            PdfSettingsDialog.XamlRoot = Content.XamlRoot;
            var dialogResult = await PdfSettingsDialog.ShowAsync();
            
            if (dialogResult != ContentDialogResult.Primary)
            {
                return; // User cancelled
            }
            
            // Save the settings for next time
            var isMetric = MarginUnitToggle.IsOn;
            var newSettings = new PdfSettings
            {
                PageSize = (PageSizeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Letter",
                Orientation = (OrientationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Portrait",
                // Always store margins in inches
                MarginTop = isMetric ? PdfSettings.CmToInches(MarginTopBox.Value) : MarginTopBox.Value,
                MarginBottom = isMetric ? PdfSettings.CmToInches(MarginBottomBox.Value) : MarginBottomBox.Value,
                MarginLeft = isMetric ? PdfSettings.CmToInches(MarginLeftBox.Value) : MarginLeftBox.Value,
                MarginRight = isMetric ? PdfSettings.CmToInches(MarginRightBox.Value) : MarginRightBox.Value,
                PrintBackgrounds = PrintBackgroundsCheckBox.IsChecked ?? true,
                PrintHeaderFooter = PrintHeaderFooterCheckBox.IsChecked ?? false,
                Scale = ScaleSlider.Value,
                UseMetricUnits = isMetric
            };
            newSettings.Save();

            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(_currentFilePath ?? "document") + ".pdf"
            };
            savePicker.FileTypeChoices.Add("PDF files", new List<string>() { ".pdf" });
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            
            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    ShowLoading(true);
                    
                    // Create print settings
                    var printSettings = MarkdownWebView.CoreWebView2.Environment.CreatePrintSettings();
                    
                    // Apply user-selected settings
                    printSettings.ShouldPrintBackgrounds = newSettings.PrintBackgrounds;
                    printSettings.ShouldPrintSelectionOnly = false;
                    printSettings.ShouldPrintHeaderAndFooter = newSettings.PrintHeaderFooter;
                    
                    // Set margins
                    printSettings.MarginTop = newSettings.MarginTop;
                    printSettings.MarginBottom = newSettings.MarginBottom;
                    printSettings.MarginLeft = newSettings.MarginLeft;
                    printSettings.MarginRight = newSettings.MarginRight;
                    
                    // Set page size and orientation
                    switch (newSettings.PageSize)
                    {
                        case "Letter":
                            if (newSettings.Orientation == "Portrait")
                            {
                                printSettings.PageWidth = 8.5;
                                printSettings.PageHeight = 11.0;
                            }
                            else
                            {
                                printSettings.PageWidth = 11.0;
                                printSettings.PageHeight = 8.5;
                            }
                            break;
                        case "A4":
                            if (newSettings.Orientation == "Portrait")
                            {
                                printSettings.PageWidth = 8.27; // 210mm in inches
                                printSettings.PageHeight = 11.69; // 297mm in inches
                            }
                            else
                            {
                                printSettings.PageWidth = 11.69;
                                printSettings.PageHeight = 8.27;
                            }
                            break;
                        case "Legal":
                            if (newSettings.Orientation == "Portrait")
                            {
                                printSettings.PageWidth = 8.5;
                                printSettings.PageHeight = 14.0;
                            }
                            else
                            {
                                printSettings.PageWidth = 14.0;
                                printSettings.PageHeight = 8.5;
                            }
                            break;
                        case "A3":
                            if (newSettings.Orientation == "Portrait")
                            {
                                printSettings.PageWidth = 11.69; // 297mm in inches
                                printSettings.PageHeight = 16.54; // 420mm in inches
                            }
                            else
                            {
                                printSettings.PageWidth = 16.54;
                                printSettings.PageHeight = 11.69;
                            }
                            break;
                    }
                    
                    // Apply scale
                    printSettings.ScaleFactor = newSettings.Scale / 100.0;
                    
                    // Print to PDF
                    var result = await MarkdownWebView.CoreWebView2.PrintToPdfAsync(file.Path, printSettings);
                    
                    ShowLoading(false);
                    
                    if (result)
                    {
                        await ShowInfoDialog("Export Complete", $"PDF file saved to {file.Path}");
                    }
                    else
                    {
                        await ShowErrorDialog("Export Failed", "Failed to save PDF. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    await ShowErrorDialog("Export Error", $"Failed to export PDF: {ex.Message}");
                }
            }
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Set current theme in combo box
            switch (_themeService.CurrentTheme)
            {
                case ElementTheme.Default:
                    ThemeComboBox.SelectedIndex = 0;
                    break;
                case ElementTheme.Light:
                    ThemeComboBox.SelectedIndex = 1;
                    break;
                case ElementTheme.Dark:
                    ThemeComboBox.SelectedIndex = 2;
                    break;
            }
            
            // Set zoom value
            DefaultZoomSlider.Value = _currentZoom;
            
            SettingsDialog.XamlRoot = Content.XamlRoot;
            var result = await SettingsDialog.ShowAsync();
            
            if (result == ContentDialogResult.Primary)
            {
                // Apply theme
                var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (Enum.TryParse<ElementTheme>(selectedTheme, out var theme))
                {
                    _themeService.SetTheme(theme);
                }
                
                // Apply zoom
                _currentZoom = DefaultZoomSlider.Value;
                UpdateZoom();
            }
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.XamlRoot = Content.XamlRoot;
            await AboutDialog.ShowAsync();
        }

        private void MarkdownWebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            // Prevent navigation to external links
            if (!args.Uri.StartsWith("data:") && !args.Uri.StartsWith("about:"))
            {
                args.Cancel = true;
                // Open in default browser
                _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
            }
        }

        #region Drag and Drop

        private void OnDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Open Markdown file";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var file = items[0] as StorageFile;
                    if (file != null && IsMarkdownFile(file.Path))
                    {
                        await LoadMarkdownFile(file.Path);
                    }
                }
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                DropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private static bool IsMarkdownFile(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".md" || extension == ".markdown" || extension == ".txt";
        }

        #endregion

        #region WebView Methods

        private async Task InitializeWebView()
        {
            await MarkdownWebView.EnsureCoreWebView2Async();
            
            // Handle messages from JavaScript
            if (MarkdownWebView.CoreWebView2 != null)
            {
                MarkdownWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Enable zoom
                MarkdownWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;
                
                // Set default zoom
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.zoom = '{_currentZoom}%'");
                
                // Enable DevTools for debugging (F12)
                MarkdownWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            }
        }

        private async void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(messageJson))
                {
                    var message = JsonSerializer.Deserialize<WebMessage>(messageJson);
                    if (message != null)
                    {
                        switch (message.Type)
                        {
                            case "openExternal":
                                if (!string.IsNullOrEmpty(message.Url))
                                {
                                    await Launcher.LaunchUriAsync(new Uri(message.Url));
                                }
                                break;
                                
                            case "taskToggle":
                                // TODO: Implement task list toggle functionality
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't show to user
                System.Diagnostics.Debug.WriteLine($"WebMessage error: {ex.Message}");
            }
        }

        private void OnThemeChanged(object? sender, ElementTheme e)
        {
            // Reload the markdown with new theme
            if (!string.IsNullOrEmpty(_currentMarkdownContent))
            {
                var html = _markdownService.ConvertToHtml(_currentMarkdownContent, _themeService.IsDarkMode);
                MarkdownWebView.NavigateToString(html);
                
                // Rebuild TOC in case theme affects it
                BuildTableOfContents(_currentMarkdownContent);
            }
        }

        #endregion

        #region File Watching

        private void SetupFileWatcher(string filePath)
        {
            // Dispose existing watcher
            _fileWatcher?.Dispose();
            
            try
            {
                var directory = System.IO.Path.GetDirectoryName(filePath);
                var fileName = System.IO.Path.GetFileName(filePath);
                
                if (!string.IsNullOrEmpty(directory))
                {
                    _fileWatcher = new FileSystemWatcher(directory, fileName)
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                    };
                    
                    _fileWatcher.Changed += OnFileChanged;
                    _fileWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"File watcher error: {ex.Message}");
            }
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce file changes
            await Task.Delay(100);
            
            // Reload file on UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    await LoadMarkdownFile(_currentFilePath);
                }
            });
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            LoadingRing.IsActive = show;
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        #endregion

        #region PDF Settings Dialog Helpers

        private void MarginUnitToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (MarginUnitToggle == null || MarginTopBox == null) return;
            
            var isMetric = MarginUnitToggle.IsOn;
            
            // Convert current values
            if (isMetric)
            {
                // Convert from inches to cm
                MarginTopBox.Value = Math.Round(PdfSettings.InchesToCm(MarginTopBox.Value), 2);
                MarginBottomBox.Value = Math.Round(PdfSettings.InchesToCm(MarginBottomBox.Value), 2);
                MarginLeftBox.Value = Math.Round(PdfSettings.InchesToCm(MarginLeftBox.Value), 2);
                MarginRightBox.Value = Math.Round(PdfSettings.InchesToCm(MarginRightBox.Value), 2);
                
                MarginsHeaderText.Text = "Margins (centimeters)";
            }
            else
            {
                // Convert from cm to inches
                MarginTopBox.Value = Math.Round(PdfSettings.CmToInches(MarginTopBox.Value), 2);
                MarginBottomBox.Value = Math.Round(PdfSettings.CmToInches(MarginBottomBox.Value), 2);
                MarginLeftBox.Value = Math.Round(PdfSettings.CmToInches(MarginLeftBox.Value), 2);
                MarginRightBox.Value = Math.Round(PdfSettings.CmToInches(MarginRightBox.Value), 2);
                
                MarginsHeaderText.Text = "Margins (inches)";
            }
            
            UpdateMarginBoxesForMetric(isMetric);
        }
        
        private void UpdateMarginBoxesForMetric(bool isMetric)
        {
            if (isMetric)
            {
                // Metric settings: 0-5 cm, 0.25 cm steps
                MarginTopBox.Maximum = 5.0;
                MarginBottomBox.Maximum = 5.0;
                MarginLeftBox.Maximum = 5.0;
                MarginRightBox.Maximum = 5.0;
                
                MarginTopBox.SmallChange = 0.25;
                MarginBottomBox.SmallChange = 0.25;
                MarginLeftBox.SmallChange = 0.25;
                MarginRightBox.SmallChange = 0.25;
                
                MarginTopBox.LargeChange = 1.0;
                MarginBottomBox.LargeChange = 1.0;
                MarginLeftBox.LargeChange = 1.0;
                MarginRightBox.LargeChange = 1.0;
                
                MarginsHeaderText.Text = "Margins (centimeters)";
            }
            else
            {
                // Imperial settings: 0-2 inches, 0.1 inch steps
                MarginTopBox.Maximum = 2.0;
                MarginBottomBox.Maximum = 2.0;
                MarginLeftBox.Maximum = 2.0;
                MarginRightBox.Maximum = 2.0;
                
                MarginTopBox.SmallChange = 0.1;
                MarginBottomBox.SmallChange = 0.1;
                MarginLeftBox.SmallChange = 0.1;
                MarginRightBox.SmallChange = 0.1;
                
                MarginTopBox.LargeChange = 0.5;
                MarginBottomBox.LargeChange = 0.5;
                MarginLeftBox.LargeChange = 0.5;
                MarginRightBox.LargeChange = 0.5;
                
                MarginsHeaderText.Text = "Margins (inches)";
            }
        }

        #endregion

        #region Table of Contents

        private void BuildTableOfContents(string markdownContent)
        {
            _tocItems.Clear();

            var headers = _markdownService.ExtractHeaders(markdownContent);
            if (headers.Count == 0)
            {
                // No headers found
                return;
            }

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

                // Find the parent for this item
                while (stack.Count > 0 && stack.Peek().Level >= header.level)
                {
                    stack.Pop();
                }

                if (stack.Count == 0)
                {
                    // This is a root item
                    rootItems.Add(newItem);
                }
                else
                {
                    // Add as child of the item on top of stack
                    stack.Peek().Children.Add(newItem);
                }

                stack.Push(newItem);
            }

            // Add root items to the observable collection
            foreach (var item in rootItems)
            {
                _tocItems.Add(item);
            }

            // Set the TreeView's ItemsSource
            TocTreeView.ItemsSource = _tocItems;
        }

        private void TocToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (TocToggleButton.IsChecked == true)
            {
                ShowTocPanel();
            }
            else
            {
                HideTocPanel();
            }
        }
        
        private void ShowTocPanel()
        {
            TocPanel.Visibility = Visibility.Visible;
            TocSplitter.Visibility = Visibility.Visible;
            TocColumn.Width = new GridLength(280);
            
            // Set up splitter events
            TocSplitter.PointerPressed += TocSplitter_PointerPressed;
            TocSplitter.PointerMoved += TocSplitter_PointerMoved;
            TocSplitter.PointerReleased += TocSplitter_PointerReleased;
            TocSplitter.PointerEntered += TocSplitter_PointerEntered;
            TocSplitter.PointerExited += TocSplitter_PointerExited;
        }
        
        private void HideTocPanel()
        {
            TocPanel.Visibility = Visibility.Collapsed;
            TocSplitter.Visibility = Visibility.Collapsed;
            TocColumn.Width = new GridLength(0);
            TocToggleButton.IsChecked = false;
            
            // Clean up splitter events
            TocSplitter.PointerPressed -= TocSplitter_PointerPressed;
            TocSplitter.PointerMoved -= TocSplitter_PointerMoved;
            TocSplitter.PointerReleased -= TocSplitter_PointerReleased;
            TocSplitter.PointerEntered -= TocSplitter_PointerEntered;
            TocSplitter.PointerExited -= TocSplitter_PointerExited;
        }

        private void CloseTocButton_Click(object sender, RoutedEventArgs e)
        {
            HideTocPanel();
        }
        
        private bool _isResizing = false;
        private double _startWidth;
        private double _startX;
        
        private void TocSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _isResizing = true;
            _startWidth = TocColumn.Width.Value;
            _startX = e.GetCurrentPoint(null).Position.X;
            TocSplitter.CapturePointer(e.Pointer);
            e.Handled = true;
        }
        
        private void TocSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isResizing)
            {
                var currentX = e.GetCurrentPoint(null).Position.X;
                var deltaX = currentX - _startX;
                var newWidth = _startWidth + deltaX;
                
                // Clamp the width between min and max
                newWidth = Math.Max(200, Math.Min(500, newWidth));
                
                TocColumn.Width = new GridLength(newWidth);
                
                // Ensure cursor stays as resize during drag
                TocSplitter.SetCursor(InputSystemCursorShape.SizeWestEast);
                
                e.Handled = true;
            }
        }
        
        private void TocSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isResizing = false;
            TocSplitter.ReleasePointerCapture(e.Pointer);
            
            // Restore cursor
            TocSplitter.ResetCursor();
            
            e.Handled = true;
        }
        
        private void TocSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Change cursor to resize
            TocSplitter.SetCursor(InputSystemCursorShape.SizeWestEast);

            var splitterLine = TocSplitter.FindName("SplitterLine") as Rectangle;
            if (splitterLine != null)
            {
                splitterLine.Width = 3;
                splitterLine.Opacity = 0.8;
            }
        }
        
        private void TocSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!_isResizing)
            {
                // Restore default cursor
                TocSplitter.ResetCursor();

                var splitterLine = TocSplitter.FindName("SplitterLine") as Rectangle;
                if (splitterLine != null)
                {
                    splitterLine.Width = 1;
                    splitterLine.Opacity = 1;
                }
            }
        }

        private void TocTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is TocItem tocItem && MarkdownWebView?.CoreWebView2 != null)
            {
                // Send message to WebView to scroll to the header
                var message = new
                {
                    type = "scrollToHeader",
                    headerId = tocItem.Id
                };

                var messageJson = JsonSerializer.Serialize(message);
                MarkdownWebView.CoreWebView2.PostWebMessageAsJson(messageJson);
            }
        }
        
        private void TocItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is TocItem tocItem)
            {
                System.Diagnostics.Debug.WriteLine($"TOC Item clicked: {tocItem.Title} (ID: {tocItem.Id})");
                
                if (MarkdownWebView?.CoreWebView2 != null)
                {
                    // Send message to WebView to scroll to the header
                    var message = new
                    {
                        type = "scrollToHeader",
                        headerId = tocItem.Id
                    };

                    var messageJson = JsonSerializer.Serialize(message);
                    MarkdownWebView.CoreWebView2.PostWebMessageAsJson(messageJson);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("WebView is not ready");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Tag is not TocItem or sender is not FrameworkElement");
            }
        }

        #endregion
    }

    // Helper class for WebView2 messages
    public class WebMessage
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("line")]
        public string? Line { get; set; }
        
        [JsonPropertyName("checked")]
        public bool Checked { get; set; }
    }
}
