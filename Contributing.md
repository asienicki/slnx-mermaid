# Contributing to slnx-mermaid

Thank you for contributing.

---

## Development Setup

### Build

From the repository root:

```bash
dotnet build
```

---

## Running the CLI in Visual Studio

When running the CLI project from Visual Studio, the executable is built to:

```
src/SlnxMermaid.CLI/bin/Debug/net10.0/SlnxMermaid.CLI.exe
```

The `slnx-mermaid.yml` file is not located in that directory, so it must be explicitly provided via command line arguments.

### Recommended: configure `launchSettings.json`

File location:

```
src/SlnxMermaid.CLI/Properties/launchSettings.json
```

Example configuration:

```json
{
  "profiles": {
    "SlnxMermaid.CLI": {
      "commandName": "Project",
      "commandLineArgs": "--config ..\\..\\..\\..\\..\\slnx-mermaid.yml"
    }
  }
}
```

The relative path is resolved from:

```
bin/Debug/net10.0/
```

It navigates back to the repository root where `slnx-mermaid.yml` is located.

---

## Running from Terminal

From the repository root:

```bash
dotnet run --project src/SlnxMermaid.CLI -- --config slnx-mermaid.yml
```

Or by executing the built binary directly:

```bash
src\SlnxMermaid.CLI\bin\Debug\net10.0\SlnxMermaid.CLI.exe --config slnx-mermaid.yml
```

---

## Notes

* Always provide `--config` when running from Visual Studio unless the config file is copied to the output directory.
* Keep paths relative to the repository root.
* Do not hardcode absolute machine-specific paths.
