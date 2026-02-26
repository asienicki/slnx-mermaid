[![CI](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml/badge.svg)](https://github.com/asienicki/slnx-mermaid/actions/workflows/CI.yml)
![License](https://img.shields.io/github/license/asienicki/slnx-mermaid)
![Last Commit](https://img.shields.io/github/last-commit/asienicki/slnx-mermaid)
![PRs](https://img.shields.io/github/issues-pr/asienicki/slnx-mermaid)
[![NuGet](https://img.shields.io/nuget/v/slnx-mermaid.svg)](https://www.nuget.org/packages/slnx-mermaid/)
[![Version](https://img.shields.io/visual-studio-marketplace/v/SharpCode.slnxmermaid?label=VS%20Marketplace)](https://marketplace.visualstudio.com/items?itemName=SharpCode.slnxmermaid)
[![Code scanning alerts](https://img.shields.io/github/issues/asienicki/slnx-mermaid/code-scanning?label=code%20scanning)](https://github.com/asienicki/slnx-mermaid/security/code-scanning)


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

### Required GitHub configuration

To publish `SharpCode.slnxmermaid` in Visual Studio Marketplace, configure:

1. **Marketplace PAT token**
   - Generate a Visual Studio Marketplace PAT with permission to manage the publisher.
   - Save it in repository secrets as `VS_MARKETPLACE_TOKEN`.

2. **Workflow permissions**
   - In GitHub repository open **Settings → Actions → General**.
   - Ensure GitHub Actions are enabled for this repository.

3. **Triggering rules**
   - Run CD from branch `master` (the workflow gates publishing on `refs/heads/master`).

> Note: VSIX publication uses `VsixPublisher.exe` on `windows-latest` runners.
