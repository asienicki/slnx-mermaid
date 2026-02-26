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
4. The diagram file will be generated.
5. Open the generated file in a Mermaid-compatible viewer or Markdown preview.

## Configuration

The extension uses the same `slnx-mermaid.yml` configuration model as the CLI.
You can keep one shared config in the solution directory (output path, filters, naming).

## Output

The extension generates a Mermaid diagram file describing:

* Project references
* Dependency relationships
* Structural overview

## Troubleshooting

* Ensure the solution builds successfully.
* Ensure project references are valid.
* Check the Output window for extension logs.

## Support

For issues or feature requests, visit the project repository.
