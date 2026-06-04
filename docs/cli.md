---
layout: default
title: CLI Guide
---

# CLI Guide

The CLI endpoint is distributed as a .NET global tool and runs the same generation pipeline used by the Visual Studio extension.

## Installation

```bash
dotnet tool install --global slnx-mermaid
```

Verify the tool is available and print the generated help text:

```bash
slnx-mermaid --help
```

`--help` is handled by Spectre.Console.Cli as a built-in help option for the configured default command.

## Basic usage

Run from a directory that contains `slnx-mermaid.yml`:

```bash
slnx-mermaid
```

or provide an explicit config path:

```bash
slnx-mermaid --config build/architecture.yml
```

The command loads the YAML configuration, resolves relative paths from the configuration file location, analyzes project references in the configured solution, prints the generated Markdown-wrapped Mermaid diagram to the console, and writes the same Markdown to `output.file`.

## CLI options

| Option            | Description                           |
| ----------------- | ------------------------------------- |
| `--config <path>` | Path to a specific configuration file |
| `-h`, `--help`    | Show help information                 |

## Configuration file resolution

Order of resolution:

1. If `--config <path>` is provided, that file is used.
2. Otherwise, the CLI looks for `slnx-mermaid.yml` in the current working directory.

If no config file is found, the process exits with an error. The default lookup does **not** currently try `slnx-mermaid.yaml`; use `--config slnx-mermaid.yaml` if your file uses that extension.

## Typical CI flow

1. Commit `slnx-mermaid.yml` to the repo.
2. Run `slnx-mermaid` from the repository root, or pass the config path explicitly.
3. Commit generated diagrams or publish them as build artifacts.

Example:

```bash
slnx-mermaid --config slnx-mermaid.yml
```
