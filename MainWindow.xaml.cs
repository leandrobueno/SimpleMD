using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using WinRT.Interop;

namespace SimpleMD
{
    public sealed partial class MainWindow : Window
    {
        private string _currentFilePath;
        private double _currentZoom = 100;

        public MainWindow()
        {
            this.InitializeComponent();
            
            // Set up the custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            
            // Initialize event handlers
            InitializeEventHandlers();
            
            // Set up drag and drop
            SetupDragAndDrop();
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
            SettingsButton.Click += SettingsButton_Click;
            AboutButton.Click += AboutButton_Click;
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
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".md");
            picker.FileTypeFilter.Add(".markdown");
            picker.FileTypeFilter.Add(".txt");

            // Initialize with the current window handle
            var window = this;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadMarkdownFile(file.Path);
            }
        }

        private async System.Threading.Tasks.Task LoadMarkdownFile(string filePath)
        {
            try
            {
                ShowLoading(true);
                
                // TODO: Read file and convert markdown to HTML
                _currentFilePath = filePath;
                
                // Update UI
                FilePathText.Text = Path.GetFileName(filePath);
                FilePathText.ToolTipService.SetToolTip(FilePathText, filePath);
                
                // Show WebView, hide welcome panel
                WelcomePanel.Visibility = Visibility.Collapsed;
                WebViewBorder.Visibility = Visibility.Visible;
                
                // TODO: Load converted HTML into WebView
                
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
            // TODO: Apply zoom to WebView
        }

        private async void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement print functionality
            await ShowInfoDialog("Print", "Print functionality will be implemented soon.");
        }

        private async void SaveAsHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement save as HTML functionality
            await ShowInfoDialog("Export", "Export functionality will be implemented soon.");
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingsDialog.ShowAsync();
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            await AboutDialog.ShowAsync();
        }

        private void MarkdownWebView_NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            // Prevent navigation to external links
            if (!args.Uri.StartsWith("data:") && !args.Uri.StartsWith("about:"))
            {
                args.Cancel = true;
                // TODO: Open in default browser
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

        private bool IsMarkdownFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".md" || extension == ".markdown" || extension == ".txt";
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            LoadingRing.IsActive = show;
        }

        private async System.Threading.Tasks.Task ShowErrorDialog(string title, string message)
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

        private async System.Threading.Tasks.Task ShowInfoDialog(string title, string message)
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
    }
}
