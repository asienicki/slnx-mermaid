---
layout: default
title: VSIX Guide
---

# VSIX Guide (Visual Studio Extension)

The VSIX endpoint lets you generate Mermaid diagrams directly from Visual Studio.

## What it does

From an opened solution (`.sln` / `.slnx`), the extension triggers diagram generation and writes Mermaid output, equivalent in purpose to the CLI endpoint.

## Installation

1. Install **SLNX Mermaid** from Visual Studio Marketplace.
2. Restart Visual Studio if required.

## Usage

1. Open a solution in Visual Studio.
2. In Solution Explorer, right-click the solution node.
3. Run the **SLNX Mermaid** command.
4. Open the generated Mermaid markdown file.

## Configuration behavior

The extension uses the same project configuration approach as the CLI workflow (solution + output + filters + naming) so teams can keep one architecture standard regardless of entry point.

## VSIX packaging note

The folder [`docs/vsix`]({{ '/vsix/' | relative_url }}) contains VSIX Marketplace metadata source files in Markdown format.

- Getting started metadata: [`docs/vsix/GETTING_STARTED.md`]({{ '/vsix/GETTING_STARTED.md' | relative_url }})
- Changelog metadata: [`docs/vsix/CHANGELOG.md`]({{ '/vsix/CHANGELOG.md' | relative_url }})
