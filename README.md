# SimpleMD - Markdown Viewer

A clean and modern Markdown viewer for Windows, built with WinUI 3 and .NET 9.

## Features

- 📝 **Real-time Markdown Preview** - See your markdown rendered instantly
- 🎨 **Syntax Highlighting** - Beautiful code blocks with language-specific highlighting
- 🌙 **Dark Mode Support** - Switch between light and dark themes
- 📁 **File Association** - Set as default viewer for .md files
- 🔄 **Auto-refresh** - Automatically reload when files change
- 🖨️ **Print Support** - Print your markdown documents
- 💾 **Export to HTML** - Save rendered markdown as HTML
- 📄 **Export to PDF** - Save your documents as PDF files with customizable settings
  - Page size options (Letter, A4, Legal, A3)
  - Portrait/Landscape orientation
  - Adjustable margins (inches or centimeters)
  - Scale control (50-150%)
  - Background printing option
  - Settings are remembered between exports
- 🔍 **Zoom Control** - Adjust viewing size for comfort
- 🎯 **Drag & Drop** - Drop markdown files to open

## Supported Markdown Features

- Headers (H1-H6)
- Bold, italic, strikethrough text
- Lists (ordered, unordered, task lists)
- Tables with alignment
- Code blocks with syntax highlighting
- Blockquotes
- Links and images
- Horizontal rules
- HTML elements
- Emoji support 😊
- And more!

## Installation

1. Clone the repository
2. Open `SimpleMD.sln` in Visual Studio 2022
3. Build and run the project

## Requirements

- Windows 10 version 1809 or later
- .NET 9.0
- Visual Studio 2022 with Windows App SDK workload

## Usage

### Opening Files
- Click the "Open" button or press `Ctrl+O`
- Drag and drop markdown files onto the window
- Double-click .md files (if set as default viewer)

### Keyboard Shortcuts
- `Ctrl+O` - Open file
- `F5` - Refresh current file
- `Ctrl+P` - Print
- `Ctrl+Shift+P` - Export to PDF with settings
- `Ctrl+Plus` - Zoom in
- `Ctrl+Minus` - Zoom out

## Architecture

- **WinUI 3** - Modern Windows UI framework
- **WebView2** - Chromium-based rendering engine
- **Markdig** - Extensible Markdown processor
- **MVVM Pattern** - Clean separation of concerns

## Logo Assets

When creating logos for SimpleMD, provide the following sizes:

### Square44x44Logo (App list icon)
- 44x44 (scale-100)
- 55x55 (scale-125) 
- 66x66 (scale-150)
- 88x88 (scale-200)
- 176x176 (scale-400)

### Square150x150Logo (Start menu tile)
- 150x150 (scale-100)
- 188x188 (scale-125)
- 225x225 (scale-150)
- 300x300 (scale-200)
- 600x600 (scale-400)

### Wide310x150Logo (Wide tile)
- 310x150 (scale-100)
- 388x188 (scale-125)
- 465x225 (scale-150)
- 620x300 (scale-200)
- 1240x600 (scale-400)

### Other Assets
- StoreLogo: 50x50
- SplashScreen: 620x300 (and scaled versions)

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- Built with [Markdig](https://github.com/xoofx/markdig)
- Syntax highlighting by [Prism.js](https://prismjs.com/)
- Icons from Windows Segoe MDL2 Assets
