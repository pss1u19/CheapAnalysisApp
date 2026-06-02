# Dev image for the Angular app. T-011 runs the app via `ng serve` (live dev
# server) rather than a production build — the source is bind-mounted and an
# anonymous volume keeps the container's Linux node_modules from being shadowed
# by the host's. A multi-stage prod build (nginx) lands with the deploy work.
FROM node:22-bookworm-slim
WORKDIR /app

# No lockfile is committed yet, so `npm install` (not `npm ci`). Copying only
# package.json first lets Docker cache the dependency layer.
COPY package.json ./
RUN npm install

# Source is overlaid by the bind mount at runtime; this COPY keeps the image
# usable standalone.
COPY . .

EXPOSE 4200

# --host 0.0.0.0 makes the dev server reachable from the host; --poll makes file
# watching reliable across the Windows -> Linux bind-mount boundary.
CMD ["npm", "start", "--", "--host", "0.0.0.0", "--poll", "2000"]
