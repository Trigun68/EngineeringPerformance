# Переезд исходников при неподвижном Docker

Дата обновления: 2026-08-19.

**Следующая сессия:** пользователь откроет Cursor из новой папки и просто напишет в чат.
Это сигнал сразу привязать Compose к новому пути (`docker compose up -d`, без `-v`) и
проверить, что том SQL тот же. См. пометку в `.cursor/rules/workspace-root.mdc`.

Исходники (git-дерево) и runtime Docker — разные вещи. Контейнеры `ep-backend`,
`ep-frontend`, `ep-sqlserver`, `ep-redis`, том `engineeringperformance_mssql_data` и
пин `172.18.0.8` в `tech-lab24-edge` остаются на хосте. Переезжает только каталог с кодом.

## Что уже зафиксировано в репозитории

| Что | Зачем |
|-----|--------|
| `name: engineeringperformance` в `docker-compose.yml` | Имя проекта не берётся из имени папки. Иначе после `mv` Compose поднял бы новый том SQL. |
| Бэкапы | Host inventory ключует `project: engineeringperformance` и volume `engineeringperformance_mssql_data`. |
| Образы backend/frontend | Код в образе, не bind-mount. Работающий стек переживает `mv` дерева. |
| Bind-mounts | Только у `db-init`: `./migrations` и `./scripts/docker-db-init.sh`. |

Единственный абсолютный путь в правилах Cursor убран: команды идут из корня открытого workspace.

## Как переносить

1. Закрыть Cursor. **Не** делать `docker compose down` и тем более `-v`.
2. Переместить **весь** каталог целиком (`mv`), включая `.env` (он не в git) и неотправленные правки.
3. Открыть Cursor → Open Folder на **новом** корне (файл `docker-compose.yml` должен быть в корне).
4. В новом корне один раз привязать Compose к новому пути:

```bash
docker compose up -d
```

Без `--build`, если код по пути не менялся. Compose обновит `working_dir` в лейблах и
пересоздаст только `db-init` (новые bind-пути). Остальные контейнеры остаются.

5. Проверка:

```bash
docker compose ls | grep engineeringperformance
# CONFIG FILES — новый путь к docker-compose.yml

docker inspect ep-backend --format '{{index .Config.Labels "com.docker.compose.project"}} {{index .Config.Labels "com.docker.compose.project.working_dir"}}'
# engineeringperformance <новый корень>

curl -sS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:8081/api/health
# 200
```

6. Если `ep-frontend` пересоздавался — обновить pin RU-edge:

```bash
bash /home/antony/services/tech-lab24/ru-edge/home-peer/refresh-edge-upstreams.sh
```

## Cursor после переезда

История агента привязана к старому пути (`~/.cursor/projects/home-antony-project-EngineeringPerformance/`).
Новая папка даст новый id проекта — прошлый чат сам не подхватится. Правила `.cursor/rules/`
едут вместе с репозиторием; этого достаточно, чтобы следующая сессия не ходила в старый путь.

## Вне репозитория (поправить, если путь сменился)

Эти файлы **не** в этом git-дереве и продолжат ссылаться на старый каталог:

| Файл | Что |
|------|-----|
| `MyAssistent/.docker/tunnel-autoheal.sh` | `EP_DIR=...` (сейчас не используется в теле скрипта) |
| `MyAssistent/doc/TUNNEL_TROUBLESHOOTING.md` | `cd …/EngineeringPerformance` |
| `MyAssistent/doc/ARCHITECTURE_EDGE_NETWORK.md` | путь к `docker-compose.yml` |
| `tech-lab24/cloudflared/HOSTNAMES.md` | `cd …/EngineeringPerformance` |

Healers поднимают контейнеры по **имени** (`ep-frontend`), не по пути исходников.
Стек жив, пока не вызван `compose up` из несуществующего каталога.

## Запрещено

- Копировать дерево без `.env` и открывать «чистый git clone» рядом с живым Docker — секреты и
  `WEB_PORT` пропадут, `compose up` может пересобрать сервисы с другим конфигом.
- Задавать `COMPOSE_PROJECT_NAME` в `.env` или в окружении Cursor.
- `docker compose down -v` «чтобы привязать заново».
