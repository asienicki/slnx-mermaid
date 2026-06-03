---
layout: default
title: Configuration Reference
---

# Configuration Reference

SLNX Mermaid uses YAML configuration.

## Example

```yaml
solution: SlnxMermaid.slnx

diagram:
  direction: TD   # TD (top-down) | LR (left-right)
  includeTransitiveDependencies: false  # false = direct only | true = direct + indirect
  orderDependenciesByDepth: true  # false = legacy alphabetical edge order | true = dependency-depth order

filters:
  exclude:
    - Tests
    - Dto
    - Enums
    - AppHost
    - ServiceDefaults
    - Seeder

ui:
  mode: dark  # dark | light
  # Optional semantic role colors. Supported palette names: blue, green, yellow, orange, pink, purple, gray, red.
  semantic:
    presentation: blue      # Api/Web/Host/Gateway projects
    application: green      # Application projects
    domain: yellow          # Domain/Core projects
    infrastructure: orange  # Infrastructure projects
    dataAccess: pink        # DataAccess/Persistence/Database/Storage projects
    tooling: purple         # CLI/Console/Tools/Seeder/Migrator projects
    tests: gray             # Tests/Test/Spec projects
  # Optional per-project overrides. Keys may be exact project names or wildcard patterns. Matching is case-sensitive.
  mappings:
    SlnxMermaid.CLI: purple
    "*Tests*": gray
    "*App*": yellow
    SlnxMermaid.Core:
      fill: "#141414"
      stroke: "#90CAF9"
      color: "#FFFFFF"

naming:
  stripPrefix: SlnxMermaid_
  aliases:
    Core: CORE
    CLI: Command Line Interface

output:
  file: docs/architecture/{date}-dependency-graph-mermaid.md
```

## Sections

### `solution`
Path to `.sln` / `.slnx` file.

### `diagram.direction`
- `TD` (top-down)
- `LR` (left-right)

### `diagram.includeTransitiveDependencies`
- `false` (default): include only direct project references.
- `true`: include both direct and indirect project dependencies.
- If omitted from `slnx-mermaid.yml`, the default remains `false` to keep diagrams focused on explicit project references.

### `diagram.orderDependenciesByDepth`
- `true` (default): emit edges by dependency depth, starting with the roots that have the longest dependency chains and walking each dependency layer before shorter roots.
- `false`: preserve the legacy alphabetical Mermaid edge order.

### `filters.exclude`
Project name patterns to omit from the graph.

### `ui.mode`
Chooses the built-in diagram color palette. Supported values are:

- `dark` (default)
- `light`

### `ui.semantic`
Overrides colors assigned automatically to semantic project roles detected from project names.

Supported palette colors are `blue`, `green`, `yellow`, `orange`, `pink`, `purple`, `gray`, and `red`. The built-in roles are:

| Role | Matching project name terms | Default color |
| ---- | --------------------------- | ------------- |
| `presentation` | `Api`, `Web`, `MinimalApi`, `Host`, `Gateway` | `blue` |
| `application` | `Application` | `green` |
| `domain` | `Domain`, `Core` | `yellow` |
| `infrastructure` | `Infrastructure` | `orange` |
| `dataAccess` | `DataAccess`, `Persistence`, `Database`, `Storage` | `pink` |
| `tooling` | `Seeder`, `Migrator`, `Tools`, `Tool`, `CLI`, `Console` | `purple` |
| `tests` | `Tests`, `Test`, `Spec`, `Specs` | `gray` |

### `ui.mappings`
Defines per-project color overrides. Mapping keys may be exact project names or wildcard patterns with `*`. Matching is case-sensitive, so `*App*` matches `MyApp.Api` but not `myapp.api`. Exact project names win over wildcard matches; if several wildcard patterns match, the most specific pattern wins.

Mapping values can be:

- a palette color name, for example `purple`
- a fill color in `#RRGGBB` format
- a style object with `fill`, `stroke`, and/or `color` fields in `#RRGGBB` format

### `naming.stripPrefix`
Removes repeated prefix from project names in diagram output.

### `naming.aliases`
Maps project names to documentation-friendly labels.

### `output.file`
Target output path for generated Mermaid markdown.

## Placeholder support

| Placeholder | Description                                       |
| ----------- | ------------------------------------------------- |
| `{date}`    | Current timestamp (format: `yyyy-MM-dd HH_mm_ss`) |

## Path resolution

Both `solution` and `output.file` support:

- relative paths (resolved from config location)
- absolute paths

Forward slashes are recommended for cross-platform compatibility.
