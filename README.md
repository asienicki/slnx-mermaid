[![CI](https://github.com/asienicki/slnx-mermaid/actions/workflows/ci.yml/badge.svg)](https://github.com/asienicki/slnx-mermaid/actions/workflows/ci.yml)
![License](https://img.shields.io/github/license/asienicki/slnx-mermaid)
![Last Commit](https://img.shields.io/github/last-commit/asienicki/slnx-mermaid)
![PRs](https://img.shields.io/github/issues-pr/asienicki/slnx-mermaid)
[![NuGet](https://img.shields.io/nuget/v/slnx-mermaid.svg)](https://www.nuget.org/packages/slnx-mermaid/)
[![Version](https://img.shields.io/visual-studio-marketplace/v/SharpCode.slnxmermaid?label=VS%20Marketplace)](https://marketplace.visualstudio.com/items?itemName=SharpCode.slnxmermaid)
[![architecture](https://img.shields.io/github/actions/workflow/status/asienicki/slnx-mermaid/check-mermaid.yml?branch=master&label=architecture&style=flat-square)](https://github.com/asienicki/slnx-mermaid/actions/workflows/check-mermaid.yml)
[![CodeQL](https://img.shields.io/github/actions/workflow/status/asienicki/slnx-mermaid/codeql.yml?label=codeql&style=flat-square)](https://github.com/asienicki/slnx-mermaid/actions/workflows/codeql.yml)


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

Repository contains two dedicated workflows for extension publication:

- **RC VSIX** (`.github/workflows/rc_vsix.yml`) — runs on push to branch `rc` and publishes package as **Preview (beta)**.
- **Release VSIX** (`.github/workflows/release_vsix.yml`) — runs on push to branch `release` and publishes stable package.

### Versioning convention

Workflows automatically generate VSIX version in format:

- `17.1.0.yyDDDrr`

Where:

- `17.1.0` — fixed base version,
- `yyDDD` — UTC date (`yy` = year, `DDD` = day of year),
- `rr` — two-digit sequence derived from GitHub Actions run number (`GITHUB_RUN_NUMBER % 100`).

Example: `17.1.0.2604601` means year `26`, day `046`, sequence `01`.

### Required GitHub configuration

To make publication work with `SharpCode.slnxmermaid` on Visual Studio Marketplace, configure:

1. **Marketplace PAT token**
   - In Visual Studio Marketplace generate Personal Access Token for publisher account `SharpCode` (or account with permissions to update this extension).
   - Store token in repository secrets as:
     - `VS_MARKETPLACE_TOKEN`

2. **Workflow permissions**
   - In GitHub repository open **Settings → Actions → General**.
   - Ensure actions are enabled and workflows from this repository can run.

3. **Branch flow**
   - Push commit to `rc` → workflow publishes **beta/preview** package.
   - Push commit to `release` → workflow publishes **release** package.

> Note: publication is done by `VsixPublisher.exe` available on `windows-latest` GitHub runner.
