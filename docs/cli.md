---
layout: default
title: CLI Guide
---

# CLI Guide

The CLI endpoint is distributed as a .NET global tool.

## Installation

```bash
dotnet tool install --global slnx-mermaid
```

Verify:

```bash
dotnet tool search slnx-mermaid
```

## Basic usage

```bash
slnx-mermaid
```

or with an explicit config path:

```bash
slnx-mermaid --config build/architecture.yml
```

## CLI options

| Option            | Description                           |
| ----------------- | ------------------------------------- |
| `--config <path>` | Path to a specific configuration file |
| `--version`       | Show tool version                     |
| `--help`          | Show help information                 |

## Configuration file resolution

Order of resolution:

1. If `--config <path>` is provided, that file is used.
2. Otherwise, current directory lookup for:
   - `slnx-mermaid.yml`
   - `slnx-mermaid.yaml`

If no config file is found, the process exits with an error.

## Typical CI flow

1. Commit `slnx-mermaid.yml` to the repo.
2. Run `slnx-mermaid` in pipeline.
3. Commit generated diagrams or publish as build artifacts.
