using System;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace SimpleMD.Models
{
    public class AppSettings
    {
        private static AppSettings? _instance;
        private readonly ApplicationDataContainer _localSettings;
        
        // Settings keys
        private const string ThemeKey = "AppTheme";
        private const string DefaultZoomKey = "DefaultZoom";
        private const string WordWrapKey = "WordWrap";
        private const string ShowLineNumbersKey = "ShowLineNumbers";
        private const string AutoSaveKey = "AutoSave";
        private const string AutoSaveIntervalKey = "AutoSaveInterval";
        private const string LastWindowWidthKey = "LastWindowWidth";
        private const string LastWindowHeightKey = "LastWindowHeight";
        private const string IsMaximizedKey = "IsMaximized";
        private const string ShowStatusBarKey = "ShowStatusBar";
        private const string ShowTocByDefaultKey = "ShowTocByDefault";
        private const string TocWidthKey = "TocWidth";
        
        // Properties
        public ElementTheme Theme { get; set; } = ElementTheme.Default;
        public double DefaultZoom { get; set; } = 100.0;
        public bool WordWrap { get; set; } = true;
        public bool ShowLineNumbers { get; set; } = true;
        public bool AutoSave { get; set; } = false;
        public int AutoSaveInterval { get; set; } = 5; // minutes
        public double LastWindowWidth { get; set; } = 1200;
        public double LastWindowHeight { get; set; } = 800;
        public bool IsMaximized { get; set; } = false;
        public bool ShowStatusBar { get; set; } = true;
        public bool ShowTocByDefault { get; set; } = false;
        public double TocWidth { get; set; } = 280;
        
        public static AppSettings Instance => _instance ??= new AppSettings();
        
        private AppSettings()
        {
            _localSettings = ApplicationData.Current.LocalSettings;
            Load();
        }
        
        public void Save()
        {
            _localSettings.Values[ThemeKey] = Theme.ToString();
            _localSettings.Values[DefaultZoomKey] = DefaultZoom;
            _localSettings.Values[WordWrapKey] = WordWrap;
            _localSettings.Values[ShowLineNumbersKey] = ShowLineNumbers;
            _localSettings.Values[AutoSaveKey] = AutoSave;
            _localSettings.Values[AutoSaveIntervalKey] = AutoSaveInterval;
            _localSettings.Values[LastWindowWidthKey] = LastWindowWidth;
            _localSettings.Values[LastWindowHeightKey] = LastWindowHeight;
            _localSettings.Values[IsMaximizedKey] = IsMaximized;
            _localSettings.Values[ShowStatusBarKey] = ShowStatusBar;
            _localSettings.Values[ShowTocByDefaultKey] = ShowTocByDefault;
            _localSettings.Values[TocWidthKey] = TocWidth;
        }
        
        private void Load()
        {
            if (_localSettings.Values.TryGetValue(ThemeKey, out var theme) &&
                Enum.TryParse<ElementTheme>(theme.ToString(), out var themeValue))
            {
                Theme = themeValue;
            }

            if (_localSettings.Values.TryGetValue(DefaultZoomKey, out var zoom))
            {
                try
                {
                    DefaultZoom = Convert.ToDouble(zoom);
                    // Validate range
                    DefaultZoom = Math.Max(50, Math.Min(200, DefaultZoom));
                }
                catch
                {
                    DefaultZoom = 100.0; // Use default on error
                }
            }

            if (_localSettings.Values.TryGetValue(WordWrapKey, out var wordWrap))
            {
                try { WordWrap = Convert.ToBoolean(wordWrap); }
                catch { WordWrap = true; }
            }

            if (_localSettings.Values.TryGetValue(ShowLineNumbersKey, out var showLineNumbers))
            {
                try { ShowLineNumbers = Convert.ToBoolean(showLineNumbers); }
                catch { ShowLineNumbers = true; }
            }

            if (_localSettings.Values.TryGetValue(AutoSaveKey, out var autoSave))
            {
                try { AutoSave = Convert.ToBoolean(autoSave); }
                catch { AutoSave = false; }
            }

            if (_localSettings.Values.TryGetValue(AutoSaveIntervalKey, out var autoSaveInterval))
            {
                try
                {
                    AutoSaveInterval = Convert.ToInt32(autoSaveInterval);
                    // Validate range (1-60 minutes)
                    AutoSaveInterval = Math.Max(1, Math.Min(60, AutoSaveInterval));
                }
                catch
                {
                    AutoSaveInterval = 5;
                }
            }

            if (_localSettings.Values.TryGetValue(LastWindowWidthKey, out var width))
            {
                try
                {
                    LastWindowWidth = Convert.ToDouble(width);
                    // Validate minimum size
                    LastWindowWidth = Math.Max(400, LastWindowWidth);
                }
                catch
                {
                    LastWindowWidth = 1200;
                }
            }

            if (_localSettings.Values.TryGetValue(LastWindowHeightKey, out var height))
            {
                try
                {
                    LastWindowHeight = Convert.ToDouble(height);
                    // Validate minimum size
                    LastWindowHeight = Math.Max(300, LastWindowHeight);
                }
                catch
                {
                    LastWindowHeight = 800;
                }
            }

            if (_localSettings.Values.TryGetValue(IsMaximizedKey, out var isMaximized))
            {
                try { IsMaximized = Convert.ToBoolean(isMaximized); }
                catch { IsMaximized = false; }
            }

            if (_localSettings.Values.TryGetValue(ShowStatusBarKey, out var showStatusBar))
            {
                try { ShowStatusBar = Convert.ToBoolean(showStatusBar); }
                catch { ShowStatusBar = true; }
            }

            if (_localSettings.Values.TryGetValue(ShowTocByDefaultKey, out var showToc))
            {
                try { ShowTocByDefault = Convert.ToBoolean(showToc); }
                catch { ShowTocByDefault = false; }
            }

            if (_localSettings.Values.TryGetValue(TocWidthKey, out var tocWidth))
            {
                try
                {
                    TocWidth = Convert.ToDouble(tocWidth);
                    // Validate range
                    TocWidth = Math.Max(200, Math.Min(500, TocWidth));
                }
                catch
                {
                    TocWidth = 280;
                }
            }
        }
    }
}
