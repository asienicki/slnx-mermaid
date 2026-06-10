[![CI](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml/badge.svg)](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml)
![License](https://img.shields.io/github/license/asienicki/slnx-mermaid)
![Last Commit](https://img.shields.io/github/last-commit/asienicki/slnx-mermaid)
![PRs](https://img.shields.io/github/issues-pr/asienicki/slnx-mermaid)
[![NuGet](https://img.shields.io/nuget/v/slnx-mermaid.svg)](https://www.nuget.org/packages/slnx-mermaid/)
[![VS Marketplace](https://img.shields.io/badge/VS%20Marketplace-17.3.4.0-blue?logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=SharpCode.slnxmermaid)
[![VS Marketplace RC](https://img.shields.io/badge/VS%20Marketplace%20RC-17.3.5.0-orange?logo=visualstudio)](https://marketplace.visualstudio.com/items?itemName=SharpCode.slnxmermaid-rc)
[![architecture](https://img.shields.io/github/actions/workflow/status/asienicki/slnx-mermaid/CI-check-mermaid.yml?branch=master&label=architecture&style=flat-square)](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI-check-mermaid.yml)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/asienicki/slnx-mermaid/CI-codeql.yml?label=codeql&style=flat-square)](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI-codeql.yml)


# 🧜 slnx-mermaid

Generate Mermaid dependency diagrams from .NET solution files (`.slnx`, `.sln`) with two entry points:

- **CLI (`dotnet tool`)** — best for local automation and CI/CD.
- **VSIX (Visual Studio extension)** — best for generating diagrams directly in Visual Studio.

Both entry points produce the same architectural output (Mermaid diagrams), but they integrate into different workflows.

## Quick links

- 📚 **Documentation home (GitHub Pages):** [`docs/index.md`](docs/index.md)
- 💻 **CLI docs:** [`docs/cli.md`](docs/cli.md)
- ⚙️ **Configuration docs:** [`docs/configuration.md`](docs/configuration.md)
- 🧩 **VSIX docs:** [`docs/vsix.md`](docs/vsix.md)
- 🚀 **GitHub Pages setup:** [`docs/github-pages.md`](docs/github-pages.md)
- 🤝 **Contributing:** [`Contributing.md`](Contributing.md)

## Fast start (CLI)

```bash
dotnet tool install --global slnx-mermaid
slnx-mermaid --config slnx-mermaid.yml
```

The CLI reads the configured `.sln` or `.slnx`, analyzes project references, and writes a Markdown file containing a fenced Mermaid graph to `output.file`. Relative `solution` and `output.file` paths are resolved from the configuration file location.


## YAML IntelliSense

Install the Red Hat YAML extension for VS Code to enable JSON Schema-based assistance for `slnx-mermaid.yml`. The repository includes a JSON Schema at [`schemas/slnx-mermaid.schema.json`](schemas/slnx-mermaid.schema.json), and VS Code uses [`.vscode/settings.json`](.vscode/settings.json) to associate it with:

- `slnx-mermaid.yml`
- `slnx-mermaid.yaml`

The schema provides autocomplete, validation, enum suggestions, defaults, and field descriptions for the supported configuration options. It is generated from the shared configuration model and its metadata.

Building `SlnxMermaid.slnx` automatically regenerates the checked-in schema through the `SlnxMermaid.SchemaGenerator` project, so no separate regeneration command is needed during the normal workflow. To regenerate only the schema, run:

```bash
dotnet run --project tools/SlnxMermaid.SchemaGenerator
```

Commit the regenerated schema together with model changes. CI verifies that a solution build leaves `schemas/slnx-mermaid.schema.json` unchanged, and the configuration test suite also compares it with generated output.

## Fast start (VSIX)

1. Install the extension from Visual Studio Marketplace.
2. Open a `.sln` / `.slnx` solution in Visual Studio.
3. Right-click the solution node in Solution Explorer.
4. Run the **SLNX Mermaid** command.
5. If `slnx-mermaid.yml` is missing next to the solution, the extension creates a starter config before generation.

## License

See [`LICENSE.txt`](LICENSE.txt).

## VSIX publication to Visual Studio Marketplace (GitHub Actions)

VSIX publishing is handled by reusable workflow building blocks, orchestrated by the manual **CD** pipeline:

- **CD entrypoint**: `.github/workflows/CD.yml` (triggered with `workflow_dispatch` from `master`).
- **VSIX reusable workflow**: `.github/workflows/CD-common-vsix.yml` (called with `channel=rc` or `channel=prod`).

### Release flow

1. Run **CD** workflow manually and choose `release_channel`: `rc`, `prod`, or `both`.
2. `CD.yml` computes a shared semantic version `17.3.x` from existing Git tags.
3. `CD-common-vsix.yml` builds and publishes VSIX to Visual Studio Marketplace:
   - `rc` channel publishes **Slnx Mermaid (RC)** preview package.
   - `prod` channel publishes stable **Slnx Mermaid** package.
