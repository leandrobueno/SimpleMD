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
            {
                var pageSizeStr = pageSize?.ToString() ?? "Letter";
                // Validate page size
                if (pageSizeStr is "Letter" or "A4" or "Legal" or "A3")
                    pdfSettings.PageSize = pageSizeStr;
                else
                    pdfSettings.PageSize = isMetricRegion ? "A4" : "Letter";
            }
            else
            {
                // Default to A4 for metric regions, Letter for US
                pdfSettings.PageSize = isMetricRegion ? "A4" : "Letter";
            }

            if (settings.Values.TryGetValue(OrientationKey, out var orientation))
            {
                var orientationStr = orientation?.ToString() ?? "Portrait";
                // Validate orientation
                pdfSettings.Orientation = orientationStr is "Portrait" or "Landscape" ? orientationStr : "Portrait";
            }

            if (settings.Values.TryGetValue(MarginTopKey, out var marginTop))
            {
                try
                {
                    pdfSettings.MarginTop = Convert.ToDouble(marginTop);
                    // Validate range (0-5 inches)
                    pdfSettings.MarginTop = Math.Max(0, Math.Min(5, pdfSettings.MarginTop));
                }
                catch { pdfSettings.MarginTop = 0.5; }
            }

            if (settings.Values.TryGetValue(MarginBottomKey, out var marginBottom))
            {
                try
                {
                    pdfSettings.MarginBottom = Convert.ToDouble(marginBottom);
                    pdfSettings.MarginBottom = Math.Max(0, Math.Min(5, pdfSettings.MarginBottom));
                }
                catch { pdfSettings.MarginBottom = 0.5; }
            }

            if (settings.Values.TryGetValue(MarginLeftKey, out var marginLeft))
            {
                try
                {
                    pdfSettings.MarginLeft = Convert.ToDouble(marginLeft);
                    pdfSettings.MarginLeft = Math.Max(0, Math.Min(5, pdfSettings.MarginLeft));
                }
                catch { pdfSettings.MarginLeft = 0.5; }
            }

            if (settings.Values.TryGetValue(MarginRightKey, out var marginRight))
            {
                try
                {
                    pdfSettings.MarginRight = Convert.ToDouble(marginRight);
                    pdfSettings.MarginRight = Math.Max(0, Math.Min(5, pdfSettings.MarginRight));
                }
                catch { pdfSettings.MarginRight = 0.5; }
            }

            if (settings.Values.TryGetValue(PrintBackgroundsKey, out var printBackgrounds))
            {
                try { pdfSettings.PrintBackgrounds = Convert.ToBoolean(printBackgrounds); }
                catch { pdfSettings.PrintBackgrounds = true; }
            }

            if (settings.Values.TryGetValue(PrintHeaderFooterKey, out var printHeaderFooter))
            {
                try { pdfSettings.PrintHeaderFooter = Convert.ToBoolean(printHeaderFooter); }
                catch { pdfSettings.PrintHeaderFooter = false; }
            }

            if (settings.Values.TryGetValue(ScaleKey, out var scale))
            {
                try
                {
                    pdfSettings.Scale = Convert.ToDouble(scale);
                    // Validate range (10-500%)
                    pdfSettings.Scale = Math.Max(10, Math.Min(500, pdfSettings.Scale));
                }
                catch { pdfSettings.Scale = 100; }
            }

            if (settings.Values.TryGetValue(MarginUnitKey, out var useMetric))
            {
                try { pdfSettings.UseMetricUnits = Convert.ToBoolean(useMetric); }
                catch { pdfSettings.UseMetricUnits = isMetricRegion; }
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
