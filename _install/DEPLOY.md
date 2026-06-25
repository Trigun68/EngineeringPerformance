# Развёртывание web-версии (EngineeringPerformance)

**Обновлено:** 2026-06-08  
**Связано:** `docker-compose.yml`, `_doc/architecture/three-tier-docker.md`

---

## Требования

- Docker 24+ и Docker Compose v2
- 4 GB RAM минимум (SQL Server + API + Nginx)
- Порт 80/8081 для web, 1433 для SQL (локально)

---

## Быстрый старт (dev/staging)

1. Скопировать `.env.example` → `.env` и задать:
   - `SQL_SA_PASSWORD` — пароль SA (сложный, ≥8 символов)
   - `JWT_SECRET` — строка ≥32 символов
   - `WEB_PORT` — например `8081`

2. Сборка и запуск:

```bash
docker compose build api web
docker compose up -d
```

3. Проверка: http://localhost:${WEB_PORT:-80} — логин `admin` / `admin` (seed).

4. API health: `GET /api/auth/me` через proxy (с Bearer после login).

---

## Переменные окружения (api)

| Переменная | Назначение | По умолчанию |
|------------|------------|--------------|
| `ConnectionStrings__DefaultConnection` | MS SQL | из compose |
| `Jwt__Secret` | Подпись JWT | **обязательно** |
| `Jwt__ExpirationMinutes` | TTL токена | 60 |
| `Jwt__Issuer` / `Jwt__Audience` | Claims | EngineeringPerformance |
| `Spnego__Enabled` | Kerberos через proxy | false |
| `Spnego__TrustedProxySecret` | Секрет X-Remote-User | при Spnego=true |
| `Cors__AllowedOrigins` | Prod CORS | пусто = dev any origin |

---

## Cloudflare Tunnel (уже развёрнут отдельно)

Второй `cloudflared` в `docker-compose` **не нужен**, если туннель уже запущен в своём контейнере.

### Два портала на одном туннеле (разведение)

На `tech-lab24-edge` работают **два** web-контейнера; у каждого свой hostname:

| Hostname | Docker URL | Проект |
|----------|------------|--------|
| `eng-perf.tech-lab24.ru` | `http://ep-web:80` | **этот** репозиторий |
| `hr-portal.tech-lab24.ru` | `http://hr-portal-web:80` | HR Portal |

Каноническая таблица и скрипт проверки: `/home/antony/services/tech-lab24/cloudflared/HOSTNAMES.md` и `scripts/verify-routes.sh`.

**Важно:** `eng-perf` не должен указывать на `hr-portal-web` или `127.0.0.1:8080` — иначе в браузере откроется Laravel/Vite HR Portal.

Достаточно проверить **Public Hostname** в Zero Trust → Networks → Tunnels:

| Поле | Значение |
|------|----------|
| Hostname | `eng-perf.tech-lab24.ru` |
| Service | HTTP |
| **URL** | см. ниже |

**URL для `tech-lab24-cloudflared` (сеть `tech-lab24-edge`):**

| Поле | Значение |
|------|----------|
| Service | HTTP |
| URL | `http://ep-web:80` |

`ep-web` подключён к внешней сети `tech-lab24-edge` в `docker-compose.yml` — туннель достучится по имени контейнера, без проброса порта на хост.

Локальный доступ с хоста по-прежнему: `http://127.0.0.1:8081` (порт из `WEB_PORT`).

Ошибка «502 / пустая страница» чаще всего из‑за неверного URL: `localhost` из контейнера туннеля ≠ хост, если сети разные.

**SSL/TLS:** режим **Full**. Стек приложения: `docker compose up -d` (без дополнительных профилей).

**Логин «Неверный логин или пароль» через туннель:** API отклоняет неизвестный `Host` (HTTP 400). В `.env` задать `ALLOWED_HOSTS` с доменом туннеля, например `eng-perf.tech-lab24.ru` (по умолчанию уже в `docker-compose.yml`).

### Ошибка в консоли: `app.css` / `app.js` / `client` — `ERR_CONNECTION_REFUSED`

Симптом: в DevTools запросы на `http://localhost:5173/...` (Vite) или страница **HR Portal**, а не Engineering Performance.

**Причина:** hostname `eng-perf.tech-lab24.ru` в Cloudflare Tunnel указывает **не на `ep-web`**, а на другой сервис (часто HR Portal на `:8080`). Laravel в dev-режиме отдаёт ссылки на Vite `localhost:5173`, недоступный из браузера.

**Проверка:**

```bash
curl -sS https://eng-perf.tech-lab24.ru/ | head -5
# Должно быть: <title>Engineering Performance</title>
# Неверно: HR Portal, @vite/client, localhost:5173
```

**Исправление (Dashboard):** Public Hostname `eng-perf.tech-lab24.ru` → URL **`http://ep-web:80`** (не `http://web:80`, не `127.0.0.1:8080`).

**Типичная ловушка `http://web:80`:** Docker Compose даёт сервису DNS-алиас по **имени сервиса**. У HR Portal сервис nginx переименован в `portal-web` (контейнер `hr-portal-web`), чтобы не занимать имя `web` на `tech-lab24-edge`. У `ep-web` алиас `web` — туннель с `http://web:80` попадает в Engineering Performance. Симптом при коллизии: API `/api/*` отвечает **410 Gone** (nginx HR Portal), справочники в UI — «Не удалось загрузить справочник». Долгосрочно в Cloudflare лучше явно указать `http://ep-web:80`.

Локально приложение работает: `http://127.0.0.1:8081`.

---

## Production (on-prem) — SPNEGO / Kerberos SSO

Для развёртывания с Windows-аутентификацией (Kerberos/SPNEGO) используйте профиль `spnego`:

```bash
# Подготовка keytab на Windows (от имени администратора домена):
# ktpass /princ HTTP/eng-perf.domain@DOMAIN.COM /mapuser spnego_svc@domain.com /pass * /out http.keytab

# Развёртывание с SPNEGO
docker compose -f docker-compose.yml -f docker-compose.spnego.yml up -d
```

### Требования для SPNEGO

| Переменная | Описание | Пример |
|------------|----------|--------|
| `KEYTAB_PATH` | Путь к keytab файлу | `/etc/krb5.keytab` |
| `KRB5_CONF_PATH` | Путь к krb5.conf | `/etc/krb5.conf` |
| `KRB5_REALM` | Active Directory realm | `CORP.EXAMPLE.COM` |
| `PROXY_TRUSTED_SECRET` | Секрет для X-Trusted-Proxy-Token | случайная строка ≥32 символов |

### Конфигурация

1. **NGINX**: использует `nginx.conf.spnego` с модулем `auth_spnego`
2. **Backend**: `Spnego__Enabled=true`, валидирует `X-Trusted-Proxy-Token`
3. **Fallback**: при ошибке Kerberos — 401 с заголовком `WWW-Authenticate: Negotiate`

### Безопасность

- `PROXY_TRUSTED_SECRET` должен совпадать между nginx и api
- Keytab должен быть доступен только root/nginx (chmod 600)
- `DevWindowsAuth__SimulatedWindowsUser` должен быть пустым в production

---

## Production (on-prem) — базовая конфигурация

1. **CORS:** задать `Cors__AllowedOrigins` (не использовать `AllowAnyOrigin` в prod).
2. **Секреты:** JWT и SQL — из vault / env, не в git.
3. **БД:** подключение к существующей схеме WPF (миграции EF не ломают legacy).
4. **TLS:** terminate на NGINX (443 → 80 в контейнере или end-to-end).

---

## Откат (rollback)

| Сценарий | Действие |
|----------|----------|
| Критический баг web | Остановить `docker compose down`; пользователи возвращаются на WPF-клиент |
| Повреждение данных | Восстановить БД из backup; web и WPF используют одну БД — согласовать окно |
| Неудачный релиз API | `docker compose pull` предыдущий tag / откат image api |

WPF-дистрибутив **не удалять** до завершения MIG4-006 (см. `_doc/architecture/wpf-reference-archive.md`).

---

## Мониторинг

- Логи API: `docker logs ep-api -f`
- Логи web/nginx: `docker logs ep-web -f`
- SQL health: `docker inspect ep-sqlserver --format '{{.State.Health.Status}}'`

---

## Smoke после деплоя

```bash
./scripts/perf-smoke.sh http://localhost:8081
```

См. также `_doc/plans/uat-phase4-checklist.md`.
