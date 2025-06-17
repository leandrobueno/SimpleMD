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
                DefaultZoom = Convert.ToDouble(zoom);
                
            if (_localSettings.Values.TryGetValue(WordWrapKey, out var wordWrap))
                WordWrap = Convert.ToBoolean(wordWrap);
                
            if (_localSettings.Values.TryGetValue(ShowLineNumbersKey, out var showLineNumbers))
                ShowLineNumbers = Convert.ToBoolean(showLineNumbers);
                
            if (_localSettings.Values.TryGetValue(AutoSaveKey, out var autoSave))
                AutoSave = Convert.ToBoolean(autoSave);
                
            if (_localSettings.Values.TryGetValue(AutoSaveIntervalKey, out var autoSaveInterval))
                AutoSaveInterval = Convert.ToInt32(autoSaveInterval);
                
            if (_localSettings.Values.TryGetValue(LastWindowWidthKey, out var width))
                LastWindowWidth = Convert.ToDouble(width);
                
            if (_localSettings.Values.TryGetValue(LastWindowHeightKey, out var height))
                LastWindowHeight = Convert.ToDouble(height);
                
            if (_localSettings.Values.TryGetValue(IsMaximizedKey, out var isMaximized))
                IsMaximized = Convert.ToBoolean(isMaximized);
                
            if (_localSettings.Values.TryGetValue(ShowStatusBarKey, out var showStatusBar))
                ShowStatusBar = Convert.ToBoolean(showStatusBar);
                
            if (_localSettings.Values.TryGetValue(ShowTocByDefaultKey, out var showToc))
                ShowTocByDefault = Convert.ToBoolean(showToc);
                
            if (_localSettings.Values.TryGetValue(TocWidthKey, out var tocWidth))
                TocWidth = Convert.ToDouble(tocWidth);
        }
    }
}
