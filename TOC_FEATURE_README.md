# SimpleMD - Table of Contents Feature

## Overview

The Table of Contents (TOC) feature in SimpleMD has been successfully implemented! This feature automatically generates a navigable tree structure from your markdown document's headers.

## How to Use

1. **Toggle TOC Panel**: Click the "Contents" button in the toolbar or use the keyboard shortcut to show/hide the TOC panel.

2. **Navigate**: Click on any item in the TOC to smoothly scroll to that section in your document.

3. **Switch Sides**: Click the arrow button in the TOC header to switch the panel between left and right sides.

## Features

### Automatic Header Detection
- All headers (H1-H6) are automatically detected
- Headers are organized in a hierarchical tree structure
- Each header level has a distinct icon

### Smart Navigation
- Smooth scrolling to selected sections
- Visual highlight effect when jumping to a header
- Maintains scroll position when toggling the TOC

### Responsive Design
- Collapsible tree nodes for better organization
- Adjustable panel width
- Works seamlessly with light and dark themes

## Implementation Details

### Files Modified/Added

1. **MainWindow.xaml.cs**
   - Added TOC event handlers
   - Implemented `BuildTableOfContents` method
   - Added navigation logic

2. **TocItem.cs** (Already existed)
   - Data model for TOC items
   - Contains header level, title, ID, and icon mapping

3. **MarkdownService.cs** (Already had TOC support)
   - `ExtractHeaders` method extracts headers from markdown
   - Generates unique IDs for each header
   - Adds IDs to HTML output for navigation

### Technical Approach

1. **Header Extraction**: Uses Markdig to parse markdown and extract all headers
2. **Tree Building**: Creates a hierarchical structure based on header levels
3. **Data Binding**: Uses WinUI 3's TreeView with ObservableCollection
4. **Navigation**: WebView2 messaging system for smooth scrolling

## Testing

Test the TOC feature with the included test documents:
- `sample.md` - General markdown showcase
- `toc-test.md` - Comprehensive TOC testing with deep nesting

## Future Enhancements

Potential improvements for future versions:
- Search/filter functionality within the TOC
- Collapse/expand all buttons
- TOC export options
- Customizable header level filtering
- Keyboard shortcuts for TOC navigation

## Known Limitations

- Very long header titles may be truncated in the TOC panel
- TOC updates when file changes, but selection state is not preserved

---

The Table of Contents feature is now fully functional and ready for use!