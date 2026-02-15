---
layout: default
title: SLNX Mermaid Documentation
---

<div class="hero">
  <h1>Architecture diagrams for .NET solutions</h1>
  <p>Generate Mermaid dependency diagrams from <code>.sln</code> and <code>.slnx</code> files using either a CLI workflow or a Visual Studio extension.</p>
  <div class="cta-row">
    <a class="btn btn--primary" href="{{ '/cli/' | relative_url }}">Get started with CLI</a>
    <a class="btn btn--secondary" href="{{ '/vsix/' | relative_url }}">Use the VSIX extension</a>
  </div>
</div>

## Documentation map

<div class="card-grid">
  <a class="card" href="{{ '/cli/' | relative_url }}">
    <h3>CLI guide</h3>
    <p>Install the global tool, run generation in CI/CD, and understand command options.</p>
  </a>
  <a class="card" href="{{ '/configuration/' | relative_url }}">
    <h3>Configuration reference</h3>
    <p>Define solution path, filters, naming aliases, output paths, and placeholders.</p>
  </a>
  <a class="card" href="{{ '/vsix/' | relative_url }}">
    <h3>VSIX guide</h3>
    <p>Generate diagrams directly from Solution Explorer inside Visual Studio.</p>
  </a>
  <a class="card" href="{{ '/github-pages/' | relative_url }}">
    <h3>GitHub Pages setup</h3>
    <p>Deploy docs from <code>docs/</code> with the existing GitHub Actions workflow.</p>
  </a>
</div>

## Choose your workflow

### Use CLI when

- You need repeatable architecture generation in CI/CD.
- You want to store diagrams as repository artifacts.
- You prefer terminal-driven automation.

### Use VSIX when

- You primarily work inside Visual Studio.
- You want one-click generation from Solution Explorer.
- You want team members to generate diagrams without CLI setup.

## Shared output

Both CLI and VSIX generate Mermaid diagrams that describe project dependency structure.
