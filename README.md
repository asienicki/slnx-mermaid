[![CI](https://github.com/asienicki/slnx-mermaid/actions/workflows/ci.yml/badge.svg)](https://github.com/asienicki/slnx-mermaid/actions/workflows/ci.yml)
![License](https://img.shields.io/github/license/asienicki/slnx-mermaid)
![Last Commit](https://img.shields.io/github/last-commit/asienicki/slnx-mermaid)
![PRs](https://img.shields.io/github/issues-pr/asienicki/slnx-mermaid)
[![NuGet](https://img.shields.io/nuget/v/slnx-mermaid.svg)](https://www.nuget.org/packages/slnx-mermaid/)

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

## Fast start (VSIX)

1. Install the extension from Visual Studio Marketplace.
2. Open a `.sln` / `.slnx` solution in Visual Studio.
3. Right-click the solution node in Solution Explorer.
4. Run the **SLNX Mermaid** command.

## Why the docs were split

The project documentation has been reorganized into `/docs` so it is easier to read in GitHub and ready for **GitHub Pages** publishing.

The root README now serves as a compact project overview, while detailed topics live in dedicated pages.

## GitHub Pages

Documentation publishing is automated via GitHub Actions.

See setup instructions: [`docs/github-pages.md`](docs/github-pages.md).

## License

See [`LICENSE.txt`](LICENSE.txt).
