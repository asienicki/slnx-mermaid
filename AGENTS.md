# Repository Guidelines

## Project Structure & Module Organization

This repository contains a .NET solution for generating Mermaid dependency diagrams from `.sln` and `.slnx` files. The primary solution is `SlnxMermaid.slnx`; use `SlnxMermaidVisualStudio.slnx` for Visual Studio extension work.

- `src/SlnxMermaid.Configuration/`: shared configuration model, metadata attributes, YAML/JSON serialization, validation services, and configuration exceptions.
- `src/SlnxMermaid.Core/`: solution/project graph analysis, filtering, naming, path helpers, and Mermaid emission logic. This project depends on `SlnxMermaid.Configuration`.
- `src/SlnxMermaid.CLI/`: `slnx-mermaid` dotnet tool entry point, Spectre.Console command wiring, and CLI package assets.
- `src/SlnxMermaid.Gui.Avalonia/`: Avalonia desktop configuration editor. It must use the shared configuration model instead of defining UI-specific config copies.
- `src/SlnxMermaidVsix/`: Visual Studio extension package, command, workflow services, resources, and VSIX manifest. This is included in `SlnxMermaidVisualStudio.slnx`, not the main solution.
- `tests/SlnxMermaid.UnitTests/`: core and CLI xUnit tests, organized by feature area, with YAML fixtures in `TestData/Config/`.
- `tests/SlnxMermaid.Configuration.Tests/`: configuration serialization and validation tests.
- `tests/SlnxMermaid.Gui.Avalonia.Tests/`: Avalonia view-model/form-builder tests for the dynamic configuration editor.
- `docs/`: user documentation for CLI, VSIX, configuration, architecture diagrams, and GitHub Pages.
- `assets/` and `src/SlnxMermaid.CLI/Assets/`: repository and package visual assets.

## Build, Test, and Development Commands

- `dotnet restore SlnxMermaid.slnx`: restore dependencies for CLI, core, configuration, GUI, and tests.
- `dotnet build SlnxMermaid.slnx --no-restore`: build the main cross-platform solution after restore.
- `dotnet test SlnxMermaid.slnx --no-build`: run all tests in the main solution after building.
- `dotnet test SlnxMermaid.slnx /p:CollectCoverage=true /p:CoverletOutput=coverage/coverage /p:CoverletOutputFormat=opencover`: run tests with Coverlet coverage output compatible with CI/Sonar flows.
- `dotnet run --project src/SlnxMermaid.CLI -- --config slnx-mermaid.yml`: run the CLI against the repository sample config.
- `dotnet run --project src/SlnxMermaid.Gui.Avalonia/SlnxMermaid.Gui.Avalonia.csproj`: run the Avalonia configuration editor.
- `dotnet build SlnxMermaidVisualStudio.slnx`: build the VSIX solution from a Visual Studio-capable environment.

## Coding Style & Naming Conventions

Use C# conventions already present in the codebase: PascalCase for public types and methods, camelCase for locals and parameters, and descriptive file names matching the primary type or test subject.

`SlnxMermaid.Core` targets `net48` and `net10.0` with C# 7.3, nullable disabled, and implicit usings disabled; avoid newer language features in shared core code. `SlnxMermaid.Configuration`, CLI, GUI, and tests target modern .NET with nullable and implicit usings enabled; follow the project file settings for language version and compiler behavior.

Keep paths relative to the repository root and avoid machine-specific absolute paths. The configuration model is centralized in `SlnxMermaid.Configuration`; do not duplicate it in CLI, GUI, VSIX, or tests. For the Avalonia editor, keep reflection-based configuration field mapping in `ConfigurationFormBuilder` and prefer MVVM view models plus XAML data templates for UI behavior.

## Testing Guidelines

Tests use xUnit with `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, and Coverlet. Add focused tests near the behavior being changed:

- Core, CLI, graph, emitter, filtering, naming, and command tests belong in `tests/SlnxMermaid.UnitTests/`.
- Configuration model, validation, and serialization tests belong in `tests/SlnxMermaid.Configuration.Tests/`.
- Avalonia dynamic form, field view-model, and shared-model integration tests belong in `tests/SlnxMermaid.Gui.Avalonia.Tests/`.

Follow the existing naming style, such as `TypeOrFeature.MethodOrScenarioTests.cs` or concise feature test names already used in the target test project. Place reusable fixtures under `tests/SlnxMermaid.UnitTests/TestData/` when they are shared with existing tests, and ensure fixture files are copied to output when needed.

## Documentation, CI, and Release Notes

Keep documentation changes aligned with the affected surface:

- CLI behavior: update `docs/cli.md` and, when relevant, `README.md`.
- Configuration schema or validation behavior: update `docs/configuration.md` and sample `slnx-mermaid.yml`.
- VSIX behavior: update `docs/vsix.md` and `docs/vsix/`.
- GUI behavior: update `src/SlnxMermaid.Gui.Avalonia/README.md` when architecture or editor behavior changes.
- Architecture diagram generation: update `docs/architecture/dependency-graph-mermaid.md` or generated Mermaid output only when intentionally changing the diagram.

GitHub Actions workflows live in `.github/workflows/`. The main CI paths include Linux .NET 10 builds/tests, VSIX build validation, Mermaid architecture checks, CodeQL, Sonar, and license checks. CD workflows cover GitHub releases, NuGet, VSIX Marketplace publication, tags, and docs pages.

## Commit & Pull Request Guidelines

Use concise imperative commit summaries, often followed by a PR number when GitHub adds one, such as `Harden MermaidEmitter validation and style output (#83)`. Keep commits focused on one behavior or documentation change. Pull requests should include a clear description, linked issue when applicable, test evidence, and screenshots or generated Mermaid output when UI, VSIX, or diagram rendering changes.

## Security & Configuration Tips

Do not commit Marketplace tokens, Sonar tokens, NuGet API keys, local secrets, or user-specific IDE settings. Use `slnx-mermaid.yml` for sample configuration and keep generated output paths predictable and relative. When touching VSIX publication, remember Marketplace publishing depends on the `VS_MARKETPLACE_TOKEN` repository secret and should be run from the configured CD workflow on `master`.
