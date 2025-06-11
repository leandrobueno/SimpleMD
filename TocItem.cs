using System.Collections.ObjectModel;

namespace SimpleMD
{
    public class TocItem
    {
        public string Title { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Id { get; set; } = string.Empty;
        public ObservableCollection<TocItem> Children { get; set; } = new ObservableCollection<TocItem>();
        
        public string HeaderIcon => Level switch
        {
            1 => "\uE8BC", // Header 1 icon
            2 => "\uE8BD", // Header 2 icon
            3 => "\uE8BE", // Header 3 icon
            4 => "\uE8BF", // Header 4 icon
            5 => "\uE8C0", // Header 5 icon
            6 => "\uE8C1", // Header 6 icon
            _ => "\uE8BC"
        };
    }
}
