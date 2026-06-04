---
layout: default
title: VSIX Guide
---

# VSIX Guide (Visual Studio Extension)

The VSIX endpoint lets you generate Mermaid diagrams directly from Visual Studio. It uses the same core generator as the CLI.

## What it does

From an opened solution (`.sln` / `.slnx`), the extension creates or loads `slnx-mermaid.yml` in the solution directory, analyzes project references, writes the configured Markdown output file, and opens it in Visual Studio.

## Installation

1. Install **SLNX Mermaid** from Visual Studio Marketplace.
2. Restart Visual Studio if required.

## Usage

1. Open a solution in Visual Studio.
2. In Solution Explorer, right-click the solution node.
3. Run the **SLNX Mermaid** command.
4. The extension writes the configured Mermaid markdown file and opens it automatically.

## Configuration behavior

The extension uses the same project configuration approach as the CLI workflow (solution, diagram, filters, UI colors, naming, and output) so teams can keep one architecture standard regardless of entry point. It always looks for `slnx-mermaid.yml` next to the loaded solution. If that file is missing, the first VSIX run creates a complete starter configuration with sample values and opens it.

## VSIX packaging note

The folder [`docs/vsix`]({{ '/vsix/' | relative_url }}) contains VSIX Marketplace metadata source files in Markdown format.

- Getting started metadata: [`docs/vsix/GETTING_STARTED.md`]({{ '/vsix/GETTING_STARTED.md' | relative_url }})
- Changelog metadata: [`docs/vsix/CHANGELOG.md`]({{ '/vsix/CHANGELOG.md' | relative_url }})
