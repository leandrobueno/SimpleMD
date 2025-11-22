using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using System.Text.Json;

namespace SimpleMD.Models
{
    public class RecentFile
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime LastOpened { get; set; }
    }

    public class RecentFilesManager
    {
        private const string RecentFilesKey = "RecentFiles";
        private const int MaxRecentFiles = 10;
        private List<RecentFile> _recentFiles;

        public IReadOnlyList<RecentFile> RecentFiles => _recentFiles.AsReadOnly();

        public RecentFilesManager()
        {
            _recentFiles = new List<RecentFile>();
            Load();
        }

        public void AddFile(string filePath)
        {
            // Validate file exists before adding
            if (!System.IO.File.Exists(filePath))
                return;

            var fileName = System.IO.Path.GetFileName(filePath);

            // Remove if already exists
            _recentFiles.RemoveAll(f => f.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));

            // Add to beginning
            _recentFiles.Insert(0, new RecentFile
            {
                Path = filePath,
                Name = fileName,
                LastOpened = DateTime.Now
            });

            // Keep only the latest files
            if (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles = _recentFiles.Take(MaxRecentFiles).ToList();
            }

            Save();
        }

        public void RemoveFile(string filePath)
        {
            _recentFiles.RemoveAll(f => f.Path.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        public void Clear()
        {
            _recentFiles.Clear();
            Save();
        }

        void Load()
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values.TryGetValue(RecentFilesKey, out var data))
                {
                    var json = data?.ToString();
                    if (!string.IsNullOrEmpty(json))
                    {
                        _recentFiles = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListRecentFile) ?? [];
                    }
                    else
                    {
                        _recentFiles = [];
                    }

                    // Validate files still exist
                    _recentFiles = _recentFiles.Where(f => System.IO.File.Exists(f.Path)).ToList();
                }
                else
                {
                    _recentFiles = [];
                }
            }
            catch
            {
                _recentFiles = [];
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_recentFiles, AppJsonContext.Default.ListRecentFile);
                ApplicationData.Current.LocalSettings.Values[RecentFilesKey] = json;
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
