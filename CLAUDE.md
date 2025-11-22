# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SimpleMD is a Markdown viewer for Windows built with WinUI 3 and .NET 8. It renders Markdown files using Markdig and displays them in a WebView2 control with live preview, theme support, and PDF/HTML export capabilities.

## Build and Development Commands

### Building the Project
```bash
# Build for x64 (Debug)
dotnet build SimpleMD.sln -c Debug /p:Platform=x64

# Build for x64 (Release)
dotnet build SimpleMD.sln -c Release /p:Platform=x64

# Build for other platforms
dotnet build SimpleMD.sln -c Debug /p:Platform=x86
dotnet build SimpleMD.sln -c Debug /p:Platform=ARM64
```

### Running the Application
```bash
# Run in Debug mode
dotnet run --project SimpleMD.csproj -c Debug /p:Platform=x64

# Or use Visual Studio to run (F5)
```

### Publishing
```bash
# Publish for x64
dotnet publish SimpleMD.csproj -c Release /p:Platform=x64

# The app will be packaged as MSIX
```

## Architecture

### Application Structure

SimpleMD uses the MVVM pattern with dependency injection:

- **App.xaml.cs**: Application entry point that configures DI container using Microsoft.Extensions.Hosting. Services are registered via `ServiceCollectionExtensions.AddSimpleMdServices()`. Has fallback initialization logic if DI fails.

- **MainWindow.xaml.cs**: Main UI window that subscribes to ViewModel events and handles WebView2 interactions. Implements `IDisposable` for proper cleanup. Does NOT implement business logic - all logic belongs in the ViewModel.

- **MainViewModel.cs**: Contains all application state and business logic. Uses `RelayCommand` for command pattern. Raises events for UI interactions that require WebView2 access (like HtmlContentChanged, PrintRequested, etc.).

### Dependency Injection Pattern

Services are registered in `Helpers/ServiceCollectionExtensions.cs`:
- Singleton services: MarkdownService, ThemeService, FileService, DialogService
- Transient ViewModels: MainViewModel
- Singleton Windows: MainWindow

Access services via `App.Current.GetService<T>()` or constructor injection.

### ViewModel-to-View Communication

The ViewModel cannot directly access WebView2 (it's a View concern). Instead:
1. ViewModel raises events (e.g., `HtmlContentChanged`, `ZoomLevelChanged`)
2. MainWindow subscribes to these events in `SubscribeToViewModelEvents()`
3. MainWindow handles WebView2 operations in response

Example: When Markdown is converted to HTML, ViewModel raises `HtmlContentChanged` event, and MainWindow calls `MarkdownWebView.NavigateToString(html)`.

### WebView2 Integration

- **Virtual Host Mapping**: Local images are loaded via `SetVirtualHostNameToFolderMapping()` using the domain `appassets.example`. The MarkdownService converts relative image paths to `https://appassets.example/{relativePath}`.

- **Theme Handling**: WebView2 content uses CSS custom properties that match the app theme. The HTML template includes `data-theme='dark'` or `data-theme='light'`. When theme changes, the entire HTML is regenerated.

- **Message Passing**: JavaScript in the HTML template can send messages back to C# via `window.chrome.webview.postMessage()`. These are handled in `MainViewModel.HandleWebMessage()`.

### Markdown Processing

The `MarkdownService` uses Markdig with these extensions:
- Advanced extensions (tables, footnotes, figures)
- Emoji and smiley support
- Task lists with checkboxes
- Diagrams (Mermaid)
- Auto-generated IDs using GitHub-style slug generation

Table of contents is built by:
1. Parsing Markdown with `Markdown.Parse()`
2. Extracting all `HeadingBlock` elements
3. Building a hierarchical tree structure in `MainViewModel.BuildTableOfContents()`
4. Binding to TreeView in the UI

### File Watching

SimpleMD monitors the open file for changes using `FileSystemWatcher`:
- Set up in `MainViewModel.SetupFileWatcher()`
- Requires `runFullTrust` capability (may fail in sandboxed environments)
- Auto-reloads file with 100ms debounce on changes
- Disposed when switching files or closing

### Settings Persistence

Settings are stored in `AppSettings` (singleton) and saved to local app data as JSON:
- Theme preference (Light/Dark/System)
- Default zoom level
- Word wrap and line numbers preferences
- TOC visibility and width

Recent files are tracked separately in `RecentFilesManager`.

## Key Dependencies

- **Microsoft.WindowsAppSDK**: WinUI 3 framework
- **Markdig**: Markdown parsing and rendering
- **Microsoft.Extensions.DependencyInjection**: Service container
- **Microsoft.Extensions.Hosting**: Application host with DI

## Important Implementation Details

### When Adding New Features

1. Add business logic to MainViewModel, not MainWindow
2. If the feature needs WebView2 interaction, raise an event from ViewModel
3. Register new services in `ServiceCollectionExtensions.cs`
4. Use `RelayCommand` for all commands, with optional `canExecute` delegates

### Theme Changes

When adding theme-sensitive features:
- Subscribe to `IThemeService.ThemeChanged` event
- Regenerate HTML when theme changes (see `OnThemeChanged` in MainViewModel)
- Use CSS custom properties in the HTML template for colors

### WebView2 Navigation

Links in Markdown are handled specially:
- Internal links (anchors): Navigate within the document
- External links: Prevent navigation and open in system browser via `Launcher.LaunchUriAsync()`
- This is implemented in `MarkdownWebView_NavigationStarting` handler

### PDF Export

PDF export uses WebView2's `PrintToPdfAsync()`:
1. User settings are managed in `PdfSettings` class
2. Settings dialog shows margin controls with both inches and centimeters
3. Page size and orientation are converted to inches for WebView2 API
4. The dialog is shown in MainWindow, but settings are loaded from `PdfSettings.Load()`

### Error Handling

The app has multiple fallback layers:
- If DI fails during App initialization, creates minimal host
- If MainWindow DI fails, falls back to manual service creation
- If that fails, shows a basic error window with instructions
- All file operations are wrapped in try-catch with user-facing error dialogs

## Development Workflow

1. Make changes to C# code
2. Build with `dotnet build` or Visual Studio
3. Debug with F5 in Visual Studio (WebView2 dev tools available in Debug builds)
4. Test theme switching and file loading edge cases
5. Verify WebView2 virtual host mapping works for local images
