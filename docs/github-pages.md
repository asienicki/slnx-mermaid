# GitHub Pages Setup

This repository is prepared to publish documentation from `docs/`.

## What is already configured

- Jekyll config in [`docs/_config.yml`](./_config.yml)
- Docs entry page in [`docs/index.md`](./index.md)
- Deployment workflow in [`.github/workflows/docs-pages.yml`](../.github/workflows/docs-pages.yml)

## Enable GitHub Pages (one-time)

1. Go to **Settings → Pages** in your GitHub repository.
2. In **Build and deployment**, set **Source** to **GitHub Actions**.
3. Save.

After that, every push to `main` that changes `docs/**` automatically deploys docs.

## Expected URL

The documentation will be available at:

`https://<owner>.github.io/<repo>/`

For this repository, it should be:

`https://asienicki.github.io/slnx-mermaid/`
