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
  orderDependenciesByRole: false  # false = legacy alphabetical edge order | true = role/topological order

filters:
  exclude:
    - Tests
    - Dto
    - Enums
    - AppHost
    - ServiceDefaults
    - Seeder

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

### `diagram.orderDependenciesByRole`
- `false` (default): preserve the legacy alphabetical Mermaid edge order.
- `true`: emit edges by project role and topological layer, starting with main entry points and placing secondary/tooling entry points last.

### `filters.exclude`
Project name patterns to omit from the graph.

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
