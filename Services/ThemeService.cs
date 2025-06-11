using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

namespace SimpleMD.Services
{
    public interface IThemeService
    {
        ElementTheme CurrentTheme { get; }
        bool IsDarkMode { get; }
        event EventHandler<ElementTheme> ThemeChanged;
        void SetTheme(ElementTheme theme);
        void Initialize(Window window);
    }

    public class ThemeService : IThemeService
    {
        private const string ThemeSettingKey = "AppTheme";
        private Window? _window;
        private ElementTheme _currentTheme = ElementTheme.Default;

        public ElementTheme CurrentTheme => _currentTheme;
        
        public bool IsDarkMode
        {
            get
            {
                if (_currentTheme == ElementTheme.Default)
                {
                    // Check system theme
                    var rootElement = _window?.Content as FrameworkElement;
                    return rootElement?.ActualTheme == ElementTheme.Dark;
                }
                return _currentTheme == ElementTheme.Dark;
            }
        }

        public event EventHandler<ElementTheme>? ThemeChanged;

        public void Initialize(Window window)
        {
            _window = window;
            
            // Load saved theme preference
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ThemeSettingKey, out var savedTheme))
            {
                if (Enum.TryParse<ElementTheme>(savedTheme.ToString(), out var theme))
                {
                    _currentTheme = theme;
                    ApplyTheme();
                }
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            _currentTheme = theme;
            
            // Save preference
            ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] = theme.ToString();
            
            // Apply theme
            ApplyTheme();
            
            // Notify listeners
            ThemeChanged?.Invoke(this, theme);
        }

        private void ApplyTheme()
        {
            if (_window?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = _currentTheme;
            }
        }
    }
}
