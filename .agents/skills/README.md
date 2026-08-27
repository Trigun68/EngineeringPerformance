# Agent skills (Engineering Performance)

Установлены через `npx skills` под стек проекта.

- **SoT (в git):** `.agents/skills/` + `skills-lock.json`
- **Cursor:** симлинки `.cursor/skills/` → `.agents/skills/` (whitelist в `.gitignore`)

## Проектный оверлей (обязателен)

| Skill | Назначение |
|-------|------------|
| `engineering-performance-code` | Требования репозитория: ASP.NET 8 + MS SQL + React/MUI/AG Grid/Zustand; запрет Postgres/Tailwind/Next по умолчанию |

## Community skills

| Источник | Skills |
|----------|--------|
| [dotnet/skills](https://github.com/dotnet/skills) | `dotnet-webapi`, `optimizing-ef-core-queries`, `run-tests`, `code-testing-agent`, `test-anti-patterns`, `authoring-github-workflows` |
| [hkarpinen/agent-skills](https://github.com/hkarpinen/agent-skills) | `docker`, `dotnet-testing`, `dotnet-authorization`, `authorization`, `testing`, `react-testing`, `react-spa`, `ci-cd` |
| [nkartik94/agentic-skills](https://github.com/nkartik94/agentic-skills) | `production-react` |
| [vercel-labs/agent-skills](https://github.com/vercel-labs/agent-skills) | `vercel-react-best-practices`, `vercel-composition-patterns` |
| [microsoft/playwright-cli](https://github.com/microsoft/playwright-cli) | `playwright-cli` |

## Скопированы из `.claude/skills` (требования к коду / архитектуре)

`architecture-principles`, `code-quality-standards`, `code-writing`, `code-reviewing`, `security-guidelines`

## Обновление

```bash
npx skills update -p -y
# или точечно:
npx skills add <owner/repo> --skill <name> --agent cursor -y
```

Lock-файл: `skills-lock.json`.
