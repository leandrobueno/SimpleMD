using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SimpleMD.Services
{
    public interface IDialogService
    {
        Task ShowErrorAsync(string title, string message, string? details = null);
        Task ShowInfoAsync(string title, string message);
        Task<bool> ShowConfirmationAsync(string title, string message, string primaryButton = "Yes", string cancelButton = "No");
    }
    
    public class DialogService : IDialogService
    {
        private XamlRoot? _xamlRoot;
        
        public void SetXamlRoot(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;
        }
        
        public async Task ShowErrorAsync(string title, string message, string? details = null)
        {
            // Try to get XamlRoot from MainWindow if not set
            if (_xamlRoot == null && App.MainWindow != null)
            {
                _xamlRoot = App.MainWindow.Content.XamlRoot;
            }
            
            if (_xamlRoot == null) return;
            
            var dialog = new ContentDialog
            {
                Title = title,
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = _xamlRoot
            };
            
            if (!string.IsNullOrEmpty(details))
            {
                var stackPanel = new StackPanel { Spacing = 12 };
                stackPanel.Children.Add(new TextBlock 
                { 
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                });
                
                var expander = new Expander
                {
                    Header = "Details",
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                
                var detailsText = new TextBlock
                {
                    Text = details,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 12,
                    Opacity = 0.8
                };
                
                var scrollViewer = new ScrollViewer
                {
                    Content = detailsText,
                    MaxHeight = 200,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                
                expander.Content = scrollViewer;
                stackPanel.Children.Add(expander);
                
                dialog.Content = stackPanel;
            }
            else
            {
                dialog.Content = new TextBlock 
                { 
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                };
            }
            
            try
            {
                await dialog.ShowAsync();
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80004005))
            {
                // Only suppress "Element not found" - dialog already showing
                System.Diagnostics.Debug.WriteLine($"Dialog already showing: {ex.Message}");
            }
        }
        
        public async Task ShowInfoAsync(string title, string message)
        {
            // Try to get XamlRoot from MainWindow if not set
            if (_xamlRoot == null && App.MainWindow != null)
            {
                _xamlRoot = App.MainWindow.Content.XamlRoot;
            }
            
            if (_xamlRoot == null) return;
            
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock 
                { 
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = _xamlRoot
            };
            
            try
            {
                await dialog.ShowAsync();
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80004005))
            {
                // Only suppress "Element not found" - dialog already showing
                System.Diagnostics.Debug.WriteLine($"Dialog already showing: {ex.Message}");
            }
        }
        
        public async Task<bool> ShowConfirmationAsync(string title, string message, string primaryButton = "Yes", string cancelButton = "No")
        {
            // Try to get XamlRoot from MainWindow if not set
            if (_xamlRoot == null && App.MainWindow != null)
            {
                _xamlRoot = App.MainWindow.Content.XamlRoot;
            }
            
            if (_xamlRoot == null) return false;
            
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock 
                { 
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = primaryButton,
                CloseButtonText = cancelButton,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = _xamlRoot
            };
            
            try
            {
                var result = await dialog.ShowAsync();
                return result == ContentDialogResult.Primary;
            }
            catch (COMException ex) when (ex.HResult == unchecked((int)0x80004005))
            {
                // Only suppress "Element not found" - dialog already showing
                System.Diagnostics.Debug.WriteLine($"Dialog already showing: {ex.Message}");
                return false;
            }
        }
    }
}
