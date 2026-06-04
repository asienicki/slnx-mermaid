---
layout: default
title: Configuration Reference
---

# Configuration Reference

SLNX Mermaid uses YAML configuration. The same model is used by the CLI and by the Visual Studio extension.

## Example

```yaml
solution: SlnxMermaid.slnx

diagram:
  direction: TD   # Passed to Mermaid graph direction; common values: TD, LR, BT, RL
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
  # Optional per-project overrides. Keys use normalized project ids: project file names without extension,
  # with dots and hyphens replaced by underscores. Wildcards use * and are case-sensitive.
  mappings:
    SlnxMermaid_CLI: purple
    "*Tests*": gray
    "*App*": yellow
    SlnxMermaid_Core:
      fill: "#141414"
      stroke: "#90CAF9"
      color: "#FFFFFF"

naming:
  stripPrefix: SlnxMermaid_
  aliases:
    Core: CORE
    CLI: CommandLineInterface

output:
  file: docs/architecture/{date}-dependency-graph-mermaid.md
```

## Sections

### `solution`
Path to a `.sln` or `.slnx` file.

The generator first tries MSBuild project graph analysis. If project evaluation fails with an MSBuild project-file error, it falls back to parsing the solution file directly and reading project references from project XML.

Supported project references in the fallback parser:

- `.slnx`: `<Project Path="..." />` entries
- `.sln`: `csproj`, `fsproj`, and `vbproj` project entries
- project files: `ProjectReference` items

### `diagram.direction`
Value emitted after Mermaid `graph`, for example `TD` or `LR`. The tool does not validate this field beyond passing the value through to Mermaid.

### `diagram.includeTransitiveDependencies`
- `false` (default): include only direct project references.
- `true`: include both direct and indirect project dependencies.
- If omitted from `slnx-mermaid.yml`, the default remains `false` to keep diagrams focused on explicit project references.

### `diagram.orderDependenciesByDepth`
- `true` (default): emit edges by dependency depth, starting with the roots that have the longest dependency chains and walking each dependency layer before shorter roots.
- `false`: preserve the legacy alphabetical Mermaid edge order.

### `filters.exclude`
Case-insensitive substring filters applied to normalized project ids before edges and styles are emitted. These are not glob patterns: `Tests` excludes any normalized project id containing `tests`, while `*Tests*` would be treated as a literal substring containing asterisks.

Normalized project ids are project file names without extension, with dots and hyphens replaced by underscores. For example, `SlnxMermaid.CLI.csproj` becomes `SlnxMermaid_CLI`.

### `ui.mode`
Chooses the built-in diagram color palette. Supported values are:

- `dark` (default)
- `light`

### `ui.semantic`
Overrides colors assigned automatically to semantic project roles detected from normalized project ids.

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

Semantic role keys are case-insensitive. Color names are resolved in the active palette selected by `ui.mode`.

### `ui.mappings`
Defines per-project color overrides. Mapping keys use normalized project ids, not display names after `naming.stripPrefix` or `naming.aliases`.

Keys may be exact ids or wildcard patterns with `*`. Matching is case-sensitive, so `*App*` matches `MyApp_Api` but not `myapp_api`. Exact ids win over wildcard matches; if several wildcard patterns match, the most specific pattern wins.

Mapping values can be:

- a palette color name, for example `purple`
- a fill color in `#RRGGBB` format
- a style object with `fill`, `stroke`, and/or `color` fields in `#RRGGBB` format

If a single hex value is used, it is treated as the fill color. Text and stroke colors are chosen automatically unless the project has a semantic base style.

### `naming.stripPrefix`
Removes a repeated prefix from normalized project ids in diagram output.

Example: with `stripPrefix: SlnxMermaid_`, `SlnxMermaid_Core` is emitted as `Core`.

### `naming.aliases`
Maps transformed output names to Mermaid node ids. Aliases are applied after project id normalization and after `stripPrefix`.

Prefer Mermaid-safe aliases made of letters, digits, and underscores. The emitter writes aliases as node ids, not quoted labels, so spaces or punctuation can make the Mermaid output invalid.

### `output.file`
Target output path for generated Mermaid markdown. The file contains a fenced Markdown code block with `mermaid` as the language.

## Placeholder support

| Placeholder | Description                                       |
| ----------- | ------------------------------------------------- |
| `{date}`    | Current timestamp (format: `yyyy-MM-dd HH_mm_ss`) |

## Path resolution

Both `solution` and `output.file` support:

- relative paths (resolved from the configuration file location)
- absolute paths

Forward slashes are recommended for cross-platform compatibility.
