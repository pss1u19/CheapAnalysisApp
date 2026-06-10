# Frontend

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.26.

## Package manager

This project uses [pnpm](https://pnpm.io) (ARCHITECTURE §5). The version is pinned by the `packageManager` field in `package.json`; [Corepack](https://nodejs.org/api/corepack.html) (bundled with Node) resolves it automatically:

```bash
corepack enable   # one-time — activates the pinned pnpm
pnpm install      # install dependencies from the committed pnpm-lock.yaml
```

`pnpm-lock.yaml` is committed, so installs are reproducible: CI and Docker run `pnpm install --frozen-lockfile` to install the exact locked versions. Native build scripts are blocked by pnpm unless allowlisted in `pnpm-workspace.yaml` (`allowBuilds`).

## Development server

To start a local development server, run:

```bash
pnpm start
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
pnpm exec ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
pnpm exec ng generate --help
```

## Building

To build the project run:

```bash
pnpm build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## UI tooling

This project is wired up with:

- **Tailwind CSS** (v3) — utility-first styling. Configured in `tailwind.config.js`; design tokens live in `src/styles.scss`.
- **Angular Material** (M3) — the theme is applied via `mat.theme(...)` in `src/styles.scss`; animations are provided by `provideAnimationsAsync()` in `app.config.ts`.
- **spartan-ng** — headless `@spartan-ng/brain` primitives plus the official Tailwind preset. The `hlm(...)` helper in `src/app/shared/utils/hlm.ts` composes Tailwind classes the way spartan `helm` components do.

## Linting and formatting

```bash
pnpm run lint          # ESLint (@angular-eslint flat config)
pnpm run lint:fix      # ESLint with autofix
pnpm run format        # Prettier --write
pnpm run format:check  # Prettier --check
```

## Running unit tests

Unit tests run on [Jest](https://jestjs.io) (via `jest-preset-angular`):

```bash
pnpm test              # run once
pnpm run test:watch    # watch mode
pnpm run test:coverage # with coverage report
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
pnpm exec ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
