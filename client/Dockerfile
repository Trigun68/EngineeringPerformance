# Stage 1: build React SPA
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci || npm install

COPY . ./
RUN npm run build

# Stage 2: nginx static + API proxy (порт 80 для Cloudflare Tunnel → http://ep-web:80)
FROM nginx:alpine AS runtime

RUN apk add --no-cache shadow libcap \
    && addgroup -S -g 10001 appuser \
    && adduser -S -u 10001 -G appuser appuser \
    && mkdir -p /tmp/client_temp /tmp/proxy_temp /tmp/fastcgi_temp /tmp/uwsgi_temp /tmp/scgi_temp \
    && chown -R appuser:appuser /usr/share/nginx/html /tmp /var/cache/nginx \
    && setcap 'cap_net_bind_service=+ep' /usr/sbin/nginx

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
RUN chown -R appuser:appuser /usr/share/nginx/html /etc/nginx/nginx.conf

USER appuser

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
