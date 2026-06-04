# 🧜 slnx-mermaid

## Getting Started (VSIX)

[![CI](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml/badge.svg)](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml)
[![Code scanning alerts](https://img.shields.io/github/issues/asienicki/slnx-mermaid/code-scanning?label=Code%20scanning%20alerts)](https://github.com/asienicki/slnx-mermaid/security/code-scanning)
[![NuGet](https://img.shields.io/nuget/v/slnx-mermaid.svg)](https://www.nuget.org/packages/slnx-mermaid/)
[![GitHub](https://img.shields.io/badge/GitHub-asienicki%2Fslnx--mermaid-181717?logo=github)](https://github.com/asienicki/slnx-mermaid)

## Overview

SLNX Mermaid generates Mermaid diagrams from Visual Studio solution files (`.sln` / `.slnx`).
It helps visualize project dependencies and architecture directly inside Visual Studio.

## Installation

1. Install the extension from Visual Studio Marketplace.
2. Restart Visual Studio if required.

## How to Use

1. Open a solution (`.sln` or `.slnx`).
2. Right-click the solution node in Solution Explorer.
3. Select the **SLNX Mermaid** command.
4. The extension generates the configured Markdown file and opens it automatically.
5. Use Visual Studio Markdown preview or another Mermaid-compatible viewer to render the diagram.

## Configuration

The extension uses the same `slnx-mermaid.yml` configuration model as the CLI.
You can keep one shared config in the solution directory (solution path, diagram settings, filters, UI colors, naming, and output path). The extension looks for `slnx-mermaid.yml` next to the loaded solution. If the file is missing, the first VSIX run creates a complete starter configuration with sample values and opens it.

## Output

The extension generates a Mermaid diagram file describing:

* Direct project references by default
* Optional transitive project dependencies when enabled in configuration
* Semantic or custom Mermaid node styling

## Troubleshooting

* Ensure the solution builds successfully.
* Ensure project references are valid.
* Check the Output window for extension logs.

## Support

For issues or feature requests, visit the project repository.
