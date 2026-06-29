# Stage 1: build React client
FROM node:20-alpine AS build
WORKDIR /app

COPY client/package.json client/package-lock.json* ./
RUN npm ci || npm install

COPY client/ ./
RUN npm run build

# Stage 2: serve with nginx
FROM nginx:alpine AS runtime

RUN apk add --no-cache gettext

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf.template /etc/nginx/nginx.conf.template
COPY scripts/nginx-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh \
    && chown -R nginx:nginx /usr/share/nginx/html

EXPOSE 80

ENTRYPOINT ["/docker-entrypoint.sh"]
