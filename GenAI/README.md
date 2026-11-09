# Task Management API (GenAI section)

REST API for a task management system, generated with Claude Code (Claude Opus 5.5) from the prompt below, then reviewed by me.

## Stack

- .NET 10 Web API (controllers), Scalar for API docs
- PostgreSQL 18 in docker-compose, Dapper for data access
- DDD layering: `Domain` → `Application` → `Infrastructure` → `Api`
- xUnit + Moq (unit), Testcontainers + WebApplicationFactory (integration), built test-first

## Running it

```bash
docker compose up -d                          # Postgres on port 5460
dotnet run --project src/TaskManagement.Api   # http://localhost:5090, docs at /scalar/v1
dotnet test                                   # needs Docker running for the integration tests
```

On startup the API creates the schema if needed and seeds three users: `alice`, `bob`, `carol`.

## Endpoints

| Method | Route | Notes |
|---|---|---|
| GET | `/api/users` | Seeded users (read-only) |
| GET | `/api/users/{id}` | 404 if unknown |
| GET | `/api/tasks?status=&responsableId=` | Both filters optional |
| GET | `/api/tasks/{id}` | 404 if unknown |
| POST | `/api/tasks` | 201 + Location. New tasks start as `Pending` |
| PUT | `/api/tasks/{id}` | Full replace, including `status` |
| DELETE | `/api/tasks/{id}` | Hard delete, 204 / 404 |

Errors are RFC 7807 ProblemDetails; validation errors return 400 with an `errors` object keyed by field.

The answer to the GenAI question (prompt, AI output, validation and corrections) is in [GENAI.md](../GENAI.md) at the root of the repo.
