# Convert SVG to PNG for Windows App Icons
# Requires Inkscape to be installed and in PATH
# Download from: https://inkscape.org/

param(
    [string]$SvgPath = "MarkdownViewerLogo.svg",
    [string]$OutputDir = "."
)

Write-Host "Starting SVG to PNG conversion..." -ForegroundColor Cyan
Write-Host "SVG Path: $SvgPath" -ForegroundColor Gray
Write-Host "Output Directory: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Check if SVG file exists
if (-not (Test-Path $SvgPath)) {
    Write-Host "ERROR: SVG file not found: $SvgPath" -ForegroundColor Red
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Gray
    Write-Host "Files in current directory:" -ForegroundColor Gray
    Get-ChildItem -Name
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    return
}

# Check if Inkscape is available
Write-Host "Checking for Inkscape..." -ForegroundColor Yellow
try {
    $inkscapeVersion = inkscape --version 2>&1
    Write-Host "Found Inkscape: $inkscapeVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Inkscape not found in PATH" -ForegroundColor Red
    Write-Host "Please install Inkscape and add it to your PATH environment variable." -ForegroundColor Yellow
    Write-Host "Download from: https://inkscape.org/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Inkscape should be accessible from command line after installation." -ForegroundColor Gray
    Write-Host "You may need to restart PowerShell after installing Inkscape." -ForegroundColor Gray
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    return
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    Write-Host "Creating output directory: $OutputDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

Write-Host ""
Write-Host "Converting $SvgPath to PNG icons..." -ForegroundColor Green
Write-Host ""

$successCount = 0
$errorCount = 0

# Function to convert with error handling
function Convert-ToIcon {
    param(
        [string]$OutputFile,
        [int]$Width,
        [int]$Height = $Width
    )
    
    $fullPath = Join-Path $OutputDir $OutputFile
    Write-Host "  Creating $OutputFile (${Width}x${Height})..." -NoNewline
    
    try {
        # Use absolute paths to avoid issues
        $svgFullPath = Resolve-Path $SvgPath
        $outputFullPath = [System.IO.Path]::GetFullPath($fullPath)
        
        # Run Inkscape with proper error handling
        $result = & inkscape --export-filename="$outputFullPath" --export-width=$Width --export-height=$Height "$svgFullPath" 2>&1
        
        if ($LASTEXITCODE -eq 0 -and (Test-Path $outputFullPath)) {
            Write-Host " Success" -ForegroundColor Green
            $script:successCount++
        } else {
            Write-Host " Failed" -ForegroundColor Red
            Write-Host "    Error: $result" -ForegroundColor Red
            $script:errorCount++
        }
    } catch {
        Write-Host " Failed" -ForegroundColor Red
        Write-Host "    Exception: $($_.Exception.Message)" -ForegroundColor Red
        $script:errorCount++
    }
}

Write-Host "Square44x44Logo (Small icons):" -ForegroundColor Cyan

# Square44x44Logo (Small icons)
$sizes44 = @(
    @{Scale="100"; Size=44},
    @{Scale="125"; Size=55},
    @{Scale="150"; Size=66},
    @{Scale="200"; Size=88},
    @{Scale="400"; Size=176}
)

foreach ($size in $sizes44) {
    Convert-ToIcon "Square44x44Logo.scale-$($size.Scale).png" $size.Size
}

# Target size 24
Convert-ToIcon "Square44x44Logo.targetsize-24_altform-unplated.png" 24

Write-Host ""
Write-Host "Square150x150Logo (Medium tiles):" -ForegroundColor Cyan

# Square150x150Logo (Medium tiles)
$sizes150 = @(
    @{Scale="100"; Size=150},
    @{Scale="125"; Size=188},
    @{Scale="150"; Size=225},
    @{Scale="200"; Size=300},
    @{Scale="400"; Size=600}
)

foreach ($size in $sizes150) {
    Convert-ToIcon "Square150x150Logo.scale-$($size.Scale).png" $size.Size
}

Write-Host ""
Write-Host "Wide310x150Logo (Wide tiles):" -ForegroundColor Cyan
Write-Host "  Note: These use wide format - may need design adjustments" -ForegroundColor Yellow

# Wide310x150Logo (Wide tiles) - Note: These need to be designed for wide format
$sizesWide = @(
    @{Scale="100"; Width=310; Height=150},
    @{Scale="125"; Width=388; Height=188},
    @{Scale="150"; Width=465; Height=225},
    @{Scale="200"; Width=620; Height=300},
    @{Scale="400"; Width=1240; Height=600}
)

foreach ($size in $sizesWide) {
    Convert-ToIcon "Wide310x150Logo.scale-$($size.Scale).png" $size.Width $size.Height
}

Write-Host ""
Write-Host "SplashScreen:" -ForegroundColor Cyan
Write-Host "  Note: These use wide format - may need design adjustments" -ForegroundColor Yellow

# SplashScreen
$sizesSplash = @(
    @{Scale="100"; Width=620; Height=300},
    @{Scale="125"; Width=775; Height=375},
    @{Scale="150"; Width=930; Height=450},
    @{Scale="200"; Width=1240; Height=600},
    @{Scale="400"; Width=2480; Height=1200}
)

foreach ($size in $sizesSplash) {
    Convert-ToIcon "SplashScreen.scale-$($size.Scale).png" $size.Width $size.Height
}

Write-Host ""
Write-Host "StoreLogo:" -ForegroundColor Cyan

# StoreLogo
Convert-ToIcon "StoreLogo.png" 50

Write-Host ""
Write-Host "Promotional Images:" -ForegroundColor Cyan
Write-Host "  Note: These are for marketing and store listings" -ForegroundColor Yellow

# Custom promotional sizes
Convert-ToIcon "Promotional_720x1080.png" 720 1080
Convert-ToIcon "Promotional_1080x1080.png" 1080 1080

Write-Host ""
Write-Host "=== CONVERSION SUMMARY ===" -ForegroundColor Cyan
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { 'Red' } else { 'Green' })
Write-Host ""

if ($errorCount -gt 0) {
    Write-Host "Some conversions failed. Common issues:" -ForegroundColor Yellow
    Write-Host "  - Inkscape version compatibility (try updating Inkscape)" -ForegroundColor Gray
    Write-Host "  - File path issues (avoid spaces or special characters)" -ForegroundColor Gray
    Write-Host "  - Permissions (try running as administrator)" -ForegroundColor Gray
    Write-Host "  - SVG file corruption (try opening in browser first)" -ForegroundColor Gray
} else {
    Write-Host "All conversions completed successfully!" -ForegroundColor Green
    Write-Host "You can now replace the PNG files in your Assets folder." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Important notes:" -ForegroundColor Yellow
Write-Host "- Wide format icons may look stretched since your SVG is square" -ForegroundColor Gray
Write-Host "- Consider creating separate wide/landscape designs for better results" -ForegroundColor Gray
Write-Host "- Test the icons in Windows to see how they look" -ForegroundColor Gray
Write-Host "- Promotional images (720x1080, 1080x1080) are for marketing use" -ForegroundColor Gray

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")