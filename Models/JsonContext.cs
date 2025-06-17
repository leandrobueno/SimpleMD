using System.Collections.Generic;
using System.Text.Json.Serialization;
using SimpleMD.ViewModels;

namespace SimpleMD.Models
{
    /// <summary>
    /// JSON serialization context for trim-safe JSON operations
    /// </summary>
    [JsonSerializable(typeof(List<RecentFile>))]
    [JsonSerializable(typeof(RecentFile))]
    [JsonSerializable(typeof(WebMessage))]
    [JsonSerializable(typeof(NavigateToHeaderMessage))]
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Message for navigating to a header in the WebView
    /// </summary>
    public class NavigateToHeaderMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "scrollToHeader";

        [JsonPropertyName("headerId")]
        public string HeaderId { get; set; } = string.Empty;
    }
}
