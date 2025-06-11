# Table of Contents Test Document

This document demonstrates the Table of Contents functionality in SimpleMD with various heading levels and structures.

## Introduction

Welcome to the comprehensive TOC test document. This file contains headers at all levels to test the hierarchical structure of the table of contents.

### Background

The table of contents feature automatically extracts all headers from your markdown document and presents them in a navigable tree structure.

#### Why Use a TOC?

A table of contents helps readers:
- Navigate long documents quickly
- Get an overview of the document structure
- Jump to specific sections

##### Benefits for Technical Documentation

Technical documents especially benefit from a TOC because they often contain:
- Multiple sections
- Code examples
- Reference materials

###### Deep Nesting Example

This is a level 6 heading, the deepest level supported by markdown.

## Main Features

SimpleMD's TOC implementation includes several key features.

### Automatic Header Detection

All headers are automatically detected and included in the TOC.

### Hierarchical Display

Headers are displayed in a tree structure that reflects their nesting.

#### Collapsible Sections

Each section with children can be expanded or collapsed.

#### Visual Indicators

Different icons are used for different header levels.

### Smooth Scrolling

Clicking on a TOC item smoothly scrolls to the corresponding section.

## Technical Implementation

This section covers the technical details of the TOC implementation.

### Markdown Parsing

The markdown content is parsed using Markdig to extract headers.

#### Header ID Generation

Each header gets a unique ID based on its text content.

##### ID Formatting Rules

- Spaces are replaced with hyphens
- Special characters are removed
- Text is converted to lowercase

### TreeView Component

The WinUI 3 TreeView component displays the TOC structure.

#### Data Binding

The TreeView is bound to an ObservableCollection of TocItem objects.

#### Item Template

Each item displays:
- An icon based on header level
- The header text

### Navigation

Navigation is handled through WebView2 messaging.

#### JavaScript Integration

The WebView listens for navigation messages and scrolls to the target header.

## Advanced Features

This section explores advanced features and future enhancements.

### Search and Filter

Future versions may include the ability to search within the TOC.

### Export Options

The TOC structure could be exported as:
- Plain text outline
- HTML navigation
- PDF bookmarks

### Customization

Users might be able to customize:
- Which header levels to include
- TOC panel width
- Display options

## Examples and Use Cases

Here are some practical examples of TOC usage.

### Academic Papers

Academic papers benefit from detailed table of contents.

#### Research Papers

Research papers typically have:
- Abstract
- Introduction
- Methodology
- Results
- Discussion
- Conclusion

#### Thesis Documents

Thesis documents often have even more complex structures.

### Technical Documentation

Technical docs need clear navigation.

#### API Documentation

API docs often have:
- Overview
- Authentication
- Endpoints
- Examples
- Error codes

#### User Guides

User guides benefit from step-by-step navigation.

## Performance Considerations

Large documents require optimization.

### Memory Usage

The TOC data structure is kept minimal.

### Rendering Performance

Only visible tree nodes are rendered.

## Accessibility

The TOC is designed with accessibility in mind.

### Keyboard Navigation

Full keyboard support for navigating the tree.

### Screen Reader Support

Proper ARIA labels and roles.

## Conclusion

The Table of Contents feature in SimpleMD provides powerful navigation capabilities for markdown documents of any size.

### Summary

Key takeaways:
- Automatic header extraction
- Hierarchical display
- Smooth navigation
- Accessible design

### Future Improvements

Planned enhancements include:
- Search functionality
- Custom filtering
- Export options

---

*Thank you for testing SimpleMD's Table of Contents feature!*