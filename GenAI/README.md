# Task Management API (GenAI section)

REST API for a task management system, generated with Claude Code (Claude Opus 5.5).

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

## Business rules

- `title` required, max 200 characters. `description` optional, max 2000.
- `status`: `Pending`, `InProgress` or `Done`.
- `dueDate` required (date only). It cannot be in the past when creating; updates may keep a past date.
- `responsableId` required and must be an existing user (400 otherwise).

## Prompt used

> Create a folder named "GenAI" with a RESTful API for a task management system.
>
> Use .NET 10 Web API (controllers), PostgreSQL in docker-compose with Dapper. Use DDD fore the design pattern and include a test project.
>
> The system will have 2 entities:
> - Users, this is a simple table (id, username, created_at) with a read-only repository, no user CRUD.
> - Tasks, this one will consist of id, title, description, status, due_date, and responsable_id. Since this is a task management system, tasks will required the full CRUD functionality with hard delete.
>
> Inlude a tests project and use TDD as part of the development. Let me know if you have any questions.

The assistant then asked clarifying questions, which were answered as follows: status values Pending/InProgress/Done; `responsable_id` required and validated against users (400 if unknown); `due_date` required and not in the past on create; seed users and expose read-only GET endpoints; task list filters by status and responsible, no paging; unit + integration tests; title ≤ 200, description optional ≤ 2000.

## Validating and correcting the AI output

What the tests caught during generation:

1. **`date` columns came back as `DateOnly`, not `DateTime`.** The first repository version assumed Npgsql returns `DateTime` for `DATE` columns. Npgsql 10 returns `DateOnly`, so every read failed with `Error parsing column 4`. The round-trip integration test caught it.
2. **Dapper can't bind `DateOnly` parameters.** After switching to `DateOnly`, inserts failed with `The member DueDate of type System.DateOnly cannot be used as a parameter value`. The fix was a small `DateOnlyTypeHandler` registered with Dapper.

Both bugs only showed up against a real Postgres, so they would have reached a mocked-only test suite unnoticed. That's why the integration tests use Testcontainers.

Other checks:

- Every layer was written test-first: tests were committed before the code, and the build or test run failed (red) before the implementation went in.
- A manual smoke test against the docker-compose database: list users, create/filter/delete a task, open Scalar.
- Reviewed the result for scope: no user CRUD (`POST /api/users` returns 405, and there's a test for it), hard delete only, and the `users` FK stops orphaned tasks.
