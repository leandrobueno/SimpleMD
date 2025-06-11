# SimpleMD TOC Test Script
# This script helps test the Table of Contents navigation

Write-Host "SimpleMD TOC Testing Guide" -ForegroundColor Cyan
Write-Host "=========================" -ForegroundColor Cyan
Write-Host ""

Write-Host "1. Build and run SimpleMD" -ForegroundColor Yellow
Write-Host "2. Open one of these test files:" -ForegroundColor Yellow
Write-Host "   - simple-test.md (basic headers)" -ForegroundColor Green
Write-Host "   - test-toc.md (simple structure)" -ForegroundColor Green
Write-Host "   - toc-test.md (complex structure)" -ForegroundColor Green
Write-Host "   - sample.md (general showcase)" -ForegroundColor Green
Write-Host ""

Write-Host "3. Click the 'Contents' button to show TOC" -ForegroundColor Yellow
Write-Host ""

Write-Host "4. Test navigation:" -ForegroundColor Yellow
Write-Host "   - Click on parent headers (those with children)" -ForegroundColor Green
Write-Host "   - Click on child headers" -ForegroundColor Green
Write-Host "   - Resize the TOC panel by dragging the splitter" -ForegroundColor Green
Write-Host ""

Write-Host "5. Debug if navigation fails:" -ForegroundColor Yellow
Write-Host "   - Press F12 in the WebView to open Developer Tools" -ForegroundColor Cyan
Write-Host "   - Go to Console tab" -ForegroundColor Cyan
Write-Host "   - Click a TOC item and check for:" -ForegroundColor Cyan
Write-Host "     * 'Received message:' (confirms click worked)" -ForegroundColor White
Write-Host "     * 'Element not found with ID:' (shows mismatch)" -ForegroundColor White
Write-Host "     * 'Available headers:' (shows actual IDs)" -ForegroundColor White
Write-Host ""

Write-Host "6. Check Visual Studio Output window for C# debug messages" -ForegroundColor Yellow
Write-Host ""

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
