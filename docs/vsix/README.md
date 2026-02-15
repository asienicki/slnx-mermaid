# VSIX Metadata Documentation

This folder contains documentation files used directly by the VSIX package metadata:

* `GETTING_STARTED.txt`
* `CHANGELOG.txt`

## Why TXT instead of Markdown?

Visual Studio extension metadata does **not support Markdown (.md)** files.
The VSIX manifest only accepts formats such as:

* `.html`
* `.htm`
* `.rtf`
* `.txt`

To keep the build process simple and avoid generating HTML during CI, the metadata documentation is stored in **plain text (.txt)** format.

## Design Decision

We intentionally:

* Avoid HTML generation in CI
* Avoid committing generated files
* Keep the source of truth simple and stable
* Ensure compatibility with Visual Studio Marketplace requirements

The `.txt` files in this folder are referenced directly in the `.vsixmanifest` file.

## Repository Documentation

Markdown documentation (e.g., the main project `README.md`) remains in the repository root for:

* GitHub rendering
* Developer documentation
* Public project overview

The files in this folder exist specifically for VSIX packaging purposes.
