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
using SimpleMD.Models;
using SimpleMD.Controls;
using SimpleMD.ViewModels;
using Windows.System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI.Core;
using SimpleMD.Helpers;
using Path = System.IO.Path;

namespace SimpleMD
{
    public sealed partial class MainWindow : Window, IDisposable
    {
        private readonly MainViewModel _viewModel;
        private readonly IDialogService _dialogService;
        private string? _lastMappedDirectory;

        public MainViewModel ViewModel => _viewModel;

        public MainWindow(MainViewModel viewModel, IDialogService dialogService)
        {
            this.InitializeComponent();

            _viewModel = viewModel;
            _dialogService = dialogService;

            // Set DataContext on the content
            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = _viewModel;
            }

            // Set up the custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Initialize event handlers
            InitializeEventHandlers();

            // Set up drag and drop
            SetupDragAndDrop();

            // Initialize WebView
            _ = InitializeWebView();

            // Subscribe to ViewModel events
            SubscribeToViewModelEvents();
        }

        public void SetInitialFile(string filePath)
        {
            _viewModel.SetInitialFile(filePath);
            
            // Load the file after WebView is fully initialized
            _ = Task.Run(async () =>
            {
                // Wait for WebView to be ready before loading file
                await DispatcherQueue.EnqueueAsync(async () =>
                {
                    await EnsureWebViewInitializedAsync();
                    await _viewModel.LoadFileAsync(filePath);
                });
            });
        }

        private void InitializeEventHandlers()
        {
            // File operations - now bound to commands
            OpenFileButton.Command = _viewModel.OpenFileCommand;
            OpenFileButtonLarge.Command = _viewModel.OpenFileCommand;
            RefreshButton.Command = _viewModel.RefreshCommand;

            // Zoom controls - now bound to commands
            ZoomInButton.Command = _viewModel.ZoomInCommand;
            ZoomOutButton.Command = _viewModel.ZoomOutCommand;

            // Secondary commands - now bound to commands
            PrintButton.Command = _viewModel.PrintCommand;
            SaveAsHtmlButton.Command = _viewModel.SaveAsHtmlCommand;
            SaveAsPdfButton.Command = _viewModel.SaveAsPdfCommand;
            SettingsButton.Click += SettingsButton_Click;
            AboutButton.Click += AboutButton_Click;

            // TOC
            TocToggleButton.Command = _viewModel.ToggleTocCommand;
            CloseTocButton.Command = _viewModel.ToggleTocCommand;

            // WebView navigation
            MarkdownWebView.NavigationStarting += MarkdownWebView_NavigationStarting;
        }

        private void SubscribeToViewModelEvents()
        {
            _viewModel.HtmlContentChanged += OnHtmlContentChanged;
            _viewModel.ZoomLevelChanged += OnZoomLevelChanged;
            _viewModel.NavigateToHeader += OnNavigateToHeader;
            _viewModel.TocVisibilityChanged += OnTocVisibilityChanged;
            _viewModel.PrintRequested += OnPrintRequested;
            _viewModel.ExportPdfRequested += OnExportPdfRequested;

            // Property changed for bindings
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void SetupDragAndDrop()
        {
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

        #region ViewModel Event Handlers

        private async void OnHtmlContentChanged(object? sender, string html)
        {
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                // Set background color before loading content to prevent white flash
                await UpdateWebViewThemeAsync();

                // Set up virtual host mapping for local images if we have a current file
                await SetupVirtualHostMapping();

                ShowLoading(false);
                MarkdownWebView.NavigateToString(html);
            }
        }

        private async void OnZoomLevelChanged(object? sender, double zoomLevel)
        {
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.zoom = '{zoomLevel}%'");
            }
        }

        private void OnNavigateToHeader(object? sender, string headerId)
        {
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                var message = new NavigateToHeaderMessage { HeaderId = headerId };
                var messageJson = JsonSerializer.Serialize(message, AppJsonContext.Default.NavigateToHeaderMessage);
                MarkdownWebView.CoreWebView2.PostWebMessageAsJson(messageJson);
            }
        }

        private void OnTocVisibilityChanged(object? sender, bool isVisible)
        {
            if (isVisible)
            {
                ShowTocPanel();
            }
            else
            {
                HideTocPanel();
            }
        }

        private async void OnPrintRequested(object? sender, EventArgs e)
        {
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync("window.print();");
            }
        }

        private async void OnExportPdfRequested(object? sender, PdfExportSettings settings)
        {
            // Handle PDF export with WebView2
            await SaveAsPdfWithSettings(settings);
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.WindowTitle):
                    Title = _viewModel.WindowTitle;
                    break;
                case nameof(MainViewModel.IsLoading):
                    ShowLoading(_viewModel.IsLoading);
                    break;
                case nameof(MainViewModel.HasDocument):
                    WelcomePanel.Visibility = _viewModel.HasDocument ? Visibility.Collapsed : Visibility.Visible;
                    WebViewBorder.Visibility = _viewModel.HasDocument ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(MainViewModel.StatusText):
                    FilePathText.Text = _viewModel.StatusText;
                    ToolTipService.SetToolTip(FilePathText, _viewModel.CurrentFilePath);
                    break;
                case nameof(MainViewModel.WordCountText):
                    WordCountText.Text = _viewModel.WordCountText;
                    break;
                case nameof(MainViewModel.ZoomLevelText):
                    ZoomLevelText.Text = _viewModel.ZoomLevelText;
                    break;
                case nameof(MainViewModel.IsTocVisible):
                    TocToggleButton.IsChecked = _viewModel.IsTocVisible;
                    break;
            }
        }

        #endregion

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = _viewModel.Settings;

            // Set current values
            switch (settings.Theme)
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

            WordWrapCheckBox.IsChecked = settings.WordWrap;
            ShowLineNumbersCheckBox.IsChecked = settings.ShowLineNumbers;
            DefaultZoomSlider.Value = settings.DefaultZoom;

            SettingsDialog.XamlRoot = Content.XamlRoot;
            var result = await SettingsDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Apply settings
                var selectedTheme = (ThemeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (Enum.TryParse<ElementTheme>(selectedTheme, out var theme))
                {
                    settings.Theme = theme;
                    var themeService = App.Current.GetService<IThemeService>();
                    themeService.SetTheme(theme);
                    
                    // Update WebView background for new theme
                    await UpdateWebViewThemeAsync();
                }

                settings.WordWrap = WordWrapCheckBox.IsChecked ?? true;
                settings.ShowLineNumbers = ShowLineNumbersCheckBox.IsChecked ?? true;
                settings.DefaultZoom = DefaultZoomSlider.Value;

                settings.Save();

                // Apply zoom if changed
                if (Math.Abs(_viewModel.ZoomLevel - settings.DefaultZoom) > 0.1)
                {
                    _viewModel.ZoomLevel = settings.DefaultZoom;
                }
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
                _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
            }
        }

        #region Drag and Drop

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (_viewModel.IsDropAllowed(e))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "Open Markdown file";
                e.DragUIOverride.IsCaptionVisible = true;
                e.DragUIOverride.IsContentVisible = true;
                e.DragUIOverride.IsGlyphVisible = true;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
            await _viewModel.DropCommand.ExecuteAsync(e);
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (_viewModel.IsDropAllowed(e))
            {
                DropOverlay.Visibility = Visibility.Visible;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region WebView Methods

        private async Task InitializeWebView()
        {
            try
            {
                await MarkdownWebView.EnsureCoreWebView2Async();

                if (MarkdownWebView.CoreWebView2 != null)
                {
                MarkdownWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                MarkdownWebView.CoreWebView2.Settings.IsZoomControlEnabled = true;

                // Set default background color to prevent white flash in dark mode
                var themeService = App.Current?.GetService<IThemeService>();
                var isDarkMode = themeService?.IsDarkMode ?? false;
                var backgroundColor = isDarkMode ? "#1e1e1e" : "#ffffff";

                MarkdownWebView.DefaultBackgroundColor = isDarkMode
                    ? Microsoft.UI.Colors.Black
                    : Microsoft.UI.Colors.White;

                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.zoom = '{_viewModel.ZoomLevel}%'");

                // Set initial background to prevent flash
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.backgroundColor = '{backgroundColor}'");

#if DEBUG
                    MarkdownWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#else
                    MarkdownWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the app
                System.Diagnostics.Debug.WriteLine($"WebView2 initialization error: {ex.Message}");

                // The app can still function without WebView2, albeit with limited functionality
                // Consider showing a message to the user about degraded functionality
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                if (!string.IsNullOrEmpty(messageJson))
                {
                    _viewModel.HandleWebMessage(messageJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebMessage error: {ex.Message}");
            }
        }

        private async Task SetupVirtualHostMapping()
        {
            try
            {
                if (MarkdownWebView?.CoreWebView2 != null && !string.IsNullOrEmpty(_viewModel.CurrentFilePath))
                {
                    var baseDirectory = Path.GetDirectoryName(_viewModel.CurrentFilePath);

                    // Only remap if directory changed
                    if (baseDirectory == _lastMappedDirectory)
                        return;

                    if (!string.IsNullOrEmpty(baseDirectory) && Directory.Exists(baseDirectory))
                    {
                        // Clear any existing virtual host mappings for our domain
                        try
                        {
                            MarkdownWebView.CoreWebView2.ClearVirtualHostNameToFolderMapping("appassets.example");
                        }
                        catch
                        {
                            // Ignore errors when clearing - might not exist
                        }

                        // Set up virtual host mapping for the base directory
                        MarkdownWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                            "appassets.example",
                            baseDirectory,
                            CoreWebView2HostResourceAccessKind.Allow);

                        _lastMappedDirectory = baseDirectory;
                        System.Diagnostics.Debug.WriteLine($"Set up virtual host mapping: appassets.example -> {baseDirectory}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up virtual host mapping: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            LoadingRing.IsActive = show;
        }

        private void ShowTocPanel()
        {
            TocPanel.Visibility = Visibility.Visible;
            TocSplitter.Visibility = Visibility.Visible;
            TocColumn.Width = new GridLength(_viewModel.TocWidth);

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

            // Clean up splitter events
            TocSplitter.PointerPressed -= TocSplitter_PointerPressed;
            TocSplitter.PointerMoved -= TocSplitter_PointerMoved;
            TocSplitter.PointerReleased -= TocSplitter_PointerReleased;
            TocSplitter.PointerEntered -= TocSplitter_PointerEntered;
            TocSplitter.PointerExited -= TocSplitter_PointerExited;
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

                // Update through ViewModel
                _viewModel.TocWidth = newWidth;
                TocColumn.Width = new GridLength(_viewModel.TocWidth);

                TocSplitter.SetCursor(InputSystemCursorShape.SizeWestEast);
                e.Handled = true;
            }
        }

        private void TocSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isResizing = false;
            TocSplitter.ReleasePointerCapture(e.Pointer);
            TocSplitter.ResetCursor();
            e.Handled = true;
        }

        private void TocSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
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
            if (args.InvokedItem is TocItem tocItem)
            {
                _viewModel.NavigateToTocItemCommand.Execute(tocItem);
            }
        }

        // This method is no longer used after simplifying the TreeView template
        // Keeping for reference in case we need to restore the button functionality
        /*
        private void TocItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is TocItem tocItem)
            {
                _viewModel.NavigateToTocItemCommand.Execute(tocItem);
            }
        }
        */

        #endregion

        #region PDF Export

        private async Task SaveAsPdfWithSettings(PdfExportSettings settings)
        {
            if (MarkdownWebView?.CoreWebView2 == null)
            {
                await _dialogService.ShowErrorAsync("Export Error", "WebView is not ready. Please try again.");
                return;
            }

            // Show PDF settings dialog
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
            MarginUnitToggle.IsOn = savedSettings.UseMetricUnits;

            if (savedSettings.UseMetricUnits)
            {
                MarginTopBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginTop), 2);
                MarginBottomBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginBottom), 2);
                MarginLeftBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginLeft), 2);
                MarginRightBox.Value = Math.Round(PdfSettings.InchesToCm(savedSettings.MarginRight), 2);
                UpdateMarginBoxesForMetric(true);
            }
            else
            {
                MarginTopBox.Value = savedSettings.MarginTop;
                MarginBottomBox.Value = savedSettings.MarginBottom;
                MarginLeftBox.Value = savedSettings.MarginLeft;
                MarginRightBox.Value = savedSettings.MarginRight;
                UpdateMarginBoxesForMetric(false);
            }

            PrintBackgroundsCheckBox.IsChecked = savedSettings.PrintBackgrounds;
            PrintHeaderFooterCheckBox.IsChecked = savedSettings.PrintHeaderFooter;
            ScaleSlider.Value = savedSettings.Scale;

            PdfSettingsDialog.XamlRoot = Content.XamlRoot;
            var dialogResult = await PdfSettingsDialog.ShowAsync();

            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            // Save and apply settings
            var isMetric = MarginUnitToggle.IsOn;
            var newSettings = new PdfSettings
            {
                PageSize = (PageSizeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Letter",
                Orientation = (OrientationComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Portrait",
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

            // Export PDF
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = Path.GetFileNameWithoutExtension(_viewModel.CurrentFilePath ?? "document") + ".pdf"
            };
            savePicker.FileTypeChoices.Add("PDF files", new List<string> { ".pdf" });

            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(savePicker, hwnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    ShowLoading(true);

                    var printSettings = MarkdownWebView.CoreWebView2.Environment.CreatePrintSettings();
                    printSettings.ShouldPrintBackgrounds = newSettings.PrintBackgrounds;
                    printSettings.ShouldPrintSelectionOnly = false;
                    printSettings.ShouldPrintHeaderAndFooter = newSettings.PrintHeaderFooter;
                    printSettings.MarginTop = newSettings.MarginTop;
                    printSettings.MarginBottom = newSettings.MarginBottom;
                    printSettings.MarginLeft = newSettings.MarginLeft;
                    printSettings.MarginRight = newSettings.MarginRight;

                    // Set page size
                    switch (newSettings.PageSize)
                    {
                        case "Letter":
                            printSettings.PageWidth = newSettings.Orientation == "Portrait" ? 8.5 : 11.0;
                            printSettings.PageHeight = newSettings.Orientation == "Portrait" ? 11.0 : 8.5;
                            break;
                        case "A4":
                            printSettings.PageWidth = newSettings.Orientation == "Portrait" ? 8.27 : 11.69;
                            printSettings.PageHeight = newSettings.Orientation == "Portrait" ? 11.69 : 8.27;
                            break;
                        case "Legal":
                            printSettings.PageWidth = newSettings.Orientation == "Portrait" ? 8.5 : 14.0;
                            printSettings.PageHeight = newSettings.Orientation == "Portrait" ? 14.0 : 8.5;
                            break;
                        case "A3":
                            printSettings.PageWidth = newSettings.Orientation == "Portrait" ? 11.69 : 16.54;
                            printSettings.PageHeight = newSettings.Orientation == "Portrait" ? 16.54 : 11.69;
                            break;
                    }

                    printSettings.ScaleFactor = newSettings.Scale / 100.0;

                    var result = await MarkdownWebView.CoreWebView2.PrintToPdfAsync(file.Path, printSettings);

                    ShowLoading(false);

                    if (result)
                    {
                        await _dialogService.ShowInfoAsync("Export Complete", $"PDF file saved to {file.Path}");
                    }
                    else
                    {
                        await _dialogService.ShowErrorAsync("Export Failed", "Failed to save PDF. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    await _dialogService.ShowErrorAsync("Export Error", $"Failed to export PDF: {ex.Message}", ex.ToString());
                }
            }
        }

        private void MarginUnitToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (MarginUnitToggle == null || MarginTopBox == null) return;

            var isMetric = MarginUnitToggle.IsOn;

            if (isMetric)
            {
                MarginTopBox.Value = Math.Round(PdfSettings.InchesToCm(MarginTopBox.Value), 2);
                MarginBottomBox.Value = Math.Round(PdfSettings.InchesToCm(MarginBottomBox.Value), 2);
                MarginLeftBox.Value = Math.Round(PdfSettings.InchesToCm(MarginLeftBox.Value), 2);
                MarginRightBox.Value = Math.Round(PdfSettings.InchesToCm(MarginRightBox.Value), 2);
                MarginsHeaderText.Text = "Margins (centimeters)";
            }
            else
            {
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
            }
            else
            {
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
            }
            MarginsHeaderText.Text = isMetric ? "Margins (centimeters)" : "Margins (inches)";
        }

        private async Task EnsureWebViewInitializedAsync()
        {
            // Ensure WebView2 is fully initialized before proceeding
            if (MarkdownWebView.CoreWebView2 == null)
            {
                await MarkdownWebView.EnsureCoreWebView2Async();
                
                // Additional small delay to ensure complete initialization
                await Task.Delay(50);
            }
        }

        private async Task UpdateWebViewThemeAsync()
        {
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                var themeService = App.Current.GetService<IThemeService>();
                var backgroundColor = themeService.IsDarkMode ? "#1e1e1e" : "#ffffff";
                
                MarkdownWebView.DefaultBackgroundColor = themeService.IsDarkMode 
                    ? Microsoft.UI.Colors.Black 
                    : Microsoft.UI.Colors.White;

                // Update existing background to prevent flash during theme switch
                await MarkdownWebView.CoreWebView2.ExecuteScriptAsync($"document.body.style.backgroundColor = '{backgroundColor}'");
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Unsubscribe from ViewModel events
            if (_viewModel != null)
            {
                _viewModel.HtmlContentChanged -= OnHtmlContentChanged;
                _viewModel.ZoomLevelChanged -= OnZoomLevelChanged;
                _viewModel.NavigateToHeader -= OnNavigateToHeader;
                _viewModel.TocVisibilityChanged -= OnTocVisibilityChanged;
                _viewModel.PrintRequested -= OnPrintRequested;
                _viewModel.ExportPdfRequested -= OnExportPdfRequested;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

                _viewModel.Dispose();
            }

            // Dispose WebView2
            if (MarkdownWebView?.CoreWebView2 != null)
            {
                MarkdownWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
            MarkdownWebView?.Close();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
