using System;
using Windows.Storage;

namespace SimpleMD.Services
{
    public class PdfSettings
    {
        private const string PageSizeKey = "PdfPageSize";
        private const string OrientationKey = "PdfOrientation";
        private const string MarginTopKey = "PdfMarginTop";
        private const string MarginBottomKey = "PdfMarginBottom";
        private const string MarginLeftKey = "PdfMarginLeft";
        private const string MarginRightKey = "PdfMarginRight";
        private const string PrintBackgroundsKey = "PdfPrintBackgrounds";
        private const string PrintHeaderFooterKey = "PdfPrintHeaderFooter";
        private const string ScaleKey = "PdfScale";

        private const string MarginUnitKey = "PdfMarginUnit";

        public string PageSize { get; set; } = "Letter";
        public string Orientation { get; set; } = "Portrait";
        public double MarginTop { get; set; } = 0.5; // Default 0.5 inches (1.27 cm)
        public double MarginBottom { get; set; } = 0.5;
        public double MarginLeft { get; set; } = 0.5;
        public double MarginRight { get; set; } = 0.5;
        public bool PrintBackgrounds { get; set; } = true;
        public bool PrintHeaderFooter { get; set; } = false;
        public double Scale { get; set; } = 100;
        public bool UseMetricUnits { get; set; } = false;

        public void Save()
        {
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values[PageSizeKey] = PageSize;
            settings.Values[OrientationKey] = Orientation;
            settings.Values[MarginTopKey] = MarginTop;
            settings.Values[MarginBottomKey] = MarginBottom;
            settings.Values[MarginLeftKey] = MarginLeft;
            settings.Values[MarginRightKey] = MarginRight;
            settings.Values[PrintBackgroundsKey] = PrintBackgrounds;
            settings.Values[PrintHeaderFooterKey] = PrintHeaderFooter;
            settings.Values[ScaleKey] = Scale;
            settings.Values[MarginUnitKey] = UseMetricUnits;
        }

        public static PdfSettings Load()
        {
            var settings = ApplicationData.Current.LocalSettings;
            var pdfSettings = new PdfSettings();

            // Auto-detect region preferences if not previously set
            var region = Windows.System.UserProfile.GlobalizationPreferences.HomeGeographicRegion;
            var isMetricRegion = region != "US" && region != "LR" && region != "MM";

            if (settings.Values.TryGetValue(PageSizeKey, out var pageSize))
                pdfSettings.PageSize = pageSize?.ToString() ?? "Letter";
            else
                // Default to A4 for metric regions, Letter for US
                pdfSettings.PageSize = isMetricRegion ? "A4" : "Letter";
                
            if (settings.Values.TryGetValue(OrientationKey, out var orientation))
                pdfSettings.Orientation = orientation?.ToString() ?? "Portrait";
            if (settings.Values.TryGetValue(MarginTopKey, out var marginTop))
                pdfSettings.MarginTop = Convert.ToDouble(marginTop);
            if (settings.Values.TryGetValue(MarginBottomKey, out var marginBottom))
                pdfSettings.MarginBottom = Convert.ToDouble(marginBottom);
            if (settings.Values.TryGetValue(MarginLeftKey, out var marginLeft))
                pdfSettings.MarginLeft = Convert.ToDouble(marginLeft);
            if (settings.Values.TryGetValue(MarginRightKey, out var marginRight))
                pdfSettings.MarginRight = Convert.ToDouble(marginRight);
            if (settings.Values.TryGetValue(PrintBackgroundsKey, out var printBackgrounds))
                pdfSettings.PrintBackgrounds = Convert.ToBoolean(printBackgrounds);
            if (settings.Values.TryGetValue(PrintHeaderFooterKey, out var printHeaderFooter))
                pdfSettings.PrintHeaderFooter = Convert.ToBoolean(printHeaderFooter);
            if (settings.Values.TryGetValue(ScaleKey, out var scale))
                pdfSettings.Scale = Convert.ToDouble(scale);
            if (settings.Values.TryGetValue(MarginUnitKey, out var useMetric))
            {
                pdfSettings.UseMetricUnits = Convert.ToBoolean(useMetric);
            }
            else
            {
                // Auto-detect based on user's region if not set
                pdfSettings.UseMetricUnits = isMetricRegion;
            }

            return pdfSettings;
        }
        
        // Helper methods for unit conversion
        public static double InchesToCm(double inches) => inches * 2.54;
        public static double CmToInches(double cm) => cm / 2.54;
    }
}
