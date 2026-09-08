# Stage 1: build React SPA
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci || npm install

COPY . ./
RUN npm run build

# Stage 2: nginx static + API proxy (порт 80 для Cloudflare Tunnel → http://ep-web:80)
FROM nginx:alpine AS runtime

# gettext — envsubst для PROXY_TRUSTED_SECRET; curl — compose healthcheck на http://localhost/
RUN apk add --no-cache shadow libcap curl gettext \
    && addgroup -S -g 10001 appuser \
    && adduser -S -u 10001 -G appuser appuser \
    && mkdir -p /tmp/client_temp /tmp/proxy_temp /tmp/fastcgi_temp /tmp/uwsgi_temp /tmp/scgi_temp \
    && chown -R appuser:appuser /usr/share/nginx/html /tmp /var/cache/nginx \
    && setcap 'cap_net_bind_service=+ep' /usr/sbin/nginx

COPY --from=build /app/dist /usr/share/nginx/html
# Runtime SoT — шаблон; client/nginx.conf остаётся справочной копией вне образа.
COPY nginx.conf.template /etc/nginx/nginx.conf.template
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh \
    && chown -R appuser:appuser /usr/share/nginx/html \
    && chown appuser:appuser /etc/nginx/nginx.conf /etc/nginx/nginx.conf.template

USER appuser

EXPOSE 80

ENTRYPOINT ["/docker-entrypoint.sh"]

