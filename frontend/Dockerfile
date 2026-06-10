# Dev image for the Angular app. T-011 runs the app via `ng serve` (live dev
# server) rather than a production build — the source is bind-mounted and an
# anonymous volume keeps the container's Linux node_modules from being shadowed
# by the host's. A multi-stage prod build (nginx) lands with the deploy work.
FROM node:22-bookworm-slim
WORKDIR /app

# pnpm is the frontend package manager (ARCHITECTURE §5). Installed via npm -g
# rather than corepack to avoid corepack's signing-key drift on newer pnpm
# releases inside the slim image — keep this version in sync with the
# "packageManager" field in package.json.
RUN npm install -g pnpm@11.5.3

# Copy the manifests first so the dependency layer caches independently of the
# source. pnpm-workspace.yaml carries the allowBuilds settings (native build
# scripts); pnpm-lock.yaml pins versions. --frozen-lockfile is the npm-ci
# equivalent and fails if the lockfile drifted from package.json.
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

# Source is overlaid by the bind mount at runtime; this COPY keeps the image
# usable standalone.
COPY . .

EXPOSE 4200

# --host 0.0.0.0 makes the dev server reachable from the host; --poll makes file
# watching reliable across the Windows -> Linux bind-mount boundary. pnpm forwards
# args after the script name straight to `ng serve` (no `--` separator needed).
CMD ["pnpm", "start", "--host", "0.0.0.0", "--poll", "2000"]
