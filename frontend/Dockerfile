# ---- Stage 1: Install dependencies and build ----
FROM node:22-alpine AS build

# Enable corepack for pnpm
RUN corepack enable && corepack prepare pnpm@12.5.1 --activate

WORKDIR /app

# Copy package manifests first for better caching
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./

# Install dependencies
RUN pnpm install --frozen-lockfile

# Copy the rest of the source code
COPY . .

# Build for production
RUN pnpm run build

# ---- Stage 2: Serve with nginx ----
FROM nginx:1.27-alpine AS final

# Remove default nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built Angular app from build stage
# Angular 22 with @angular/build outputs to dist/<project-name>/browser
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
