# SLNX Mermaid Documentation

Welcome to the SLNX Mermaid docs.

SLNX Mermaid generates Mermaid dependency diagrams from `.sln` and `.slnx` solutions. You can use it in two ways:

- **CLI (`dotnet tool`)** for scripting and CI/CD.
- **VSIX (Visual Studio extension)** for IDE-first workflow.

## Documentation map

- [CLI usage](./cli.md)
- [Configuration reference](./configuration.md)
- [VSIX usage](./vsix.md)
- [GitHub Pages setup](./github-pages.md)

## Which endpoint should I use?

### Use CLI when:

- You need repeatable architecture generation in CI/CD.
- You want to store diagrams as repository artifacts.
- You prefer terminal-driven automation.

### Use VSIX when:

- You primarily work inside Visual Studio.
- You want one-click generation from Solution Explorer.
- You want team members to generate diagrams without CLI setup.

## Shared output

Both CLI and VSIX generate Mermaid diagrams that describe project dependency structure.
