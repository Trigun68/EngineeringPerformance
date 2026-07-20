#!/bin/sh
set -e
# Пустое значение ломает proxy_set_header; API игнорирует заголовок при Spnego:Enabled=false
export PROXY_TRUSTED_SECRET="${PROXY_TRUSTED_SECRET:-__UNSET__}"
envsubst '${PROXY_TRUSTED_SECRET}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf
exec nginx -g 'daemon off;'
