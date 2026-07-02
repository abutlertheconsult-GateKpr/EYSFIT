# EYSFIT

CrossFit-style PWA workout tracker. Logs sessions, tracks body metrics, and generates AI prompts for coaching.

Quick start

- Serve locally for testing: `python -m http.server 8000` or `php -S localhost:8000` or use a static server.
- Open http://localhost:8000/ and use the Install button when prompted.

Deploy to GitHub Pages (project site)

1. Create a GitHub repo named `EYSFIT`.
2. Push this project to the repo's `main` branch.
3. In GitHub repo Settings → Pages, set Source to `main` (root).
4. After publishing, the site will be available at `https://<your-user>.github.io/EYSFIT/`.

Notes

- Manifest and service worker are configured for a GitHub Pages project site (`/EYSFIT` base).
- The app includes an in-app Install CTA. Service worker registration allows localhost and HTTPS testing.

If you want, I can prepare the exact push commands or walk you through using the VS Code/GitHub Desktop UI to publish.