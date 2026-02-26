---
layout: default
title: GitHub Pages Setup
---

# GitHub Pages Setup

This repository is prepared to publish documentation from `docs/`.

## What is already configured

- Jekyll config in [`docs/_config.yml`]({{ '/_config.yml' | relative_url }})
- Docs entry page in [`docs/index.md`]({{ '/' | relative_url }})
- Deployment workflow in [`.github/workflows/CD-docs-pages.yml`](https://github.com/asienicki/slnx-mermaid/blob/master/.github/workflows/CD-docs-pages.yml), orchestrated by [`.github/workflows/CD.yml`](https://github.com/asienicki/slnx-mermaid/blob/master/.github/workflows/CD.yml)

## Enable GitHub Pages (one-time)

1. Go to **Settings → Pages** in your GitHub repository.
2. In **Build and deployment**, set **Source** to **GitHub Actions**.
3. Save.

After that, run the **CD** workflow (`.github/workflows/CD.yml`) with `deploy_docs=true` from `master` to publish docs.

## Expected URL

The documentation will be available at:

`https://<owner>.github.io/<repo>/`

For this repository, it should be:

`https://asienicki.github.io/slnx-mermaid/`
