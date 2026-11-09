# Record Label Catalog

A small full-stack app for a record label's catalog: an ASP.NET Core Web API with hand-written SQL on PostgreSQL, JWT auth, and an Angular + Tailwind frontend.

This repo has both parts of the exercise:

- **[PRESENTATION.md](PRESENTATION.md):** my thought process during the exercise. Start here.
- **Full-stack app:** `backend/` and `frontend/`, described below and in [ARCHITECTURE.md](ARCHITECTURE.md).
- **GenAI exercise:** answered in [GENAI.md](GENAI.md), code in `GenAI/`. A task-management API generated with Claude Code, with the prompt, validation and corrections.

## User story

> As the label's catalog admin, I want to log in and add, edit and remove releases (title, artist, release year, format) so that our catalog stays up to date.

Acceptance criteria:

- I can create an account, log in and log out. A demo account exists out of the box.
- Only logged-in users can see or change the catalog.
- I can search releases by title or artist and page through the results.
- A release needs a title and an artist (max 200 characters each), a year between 1900 and next year, and a format: Single, EP or Album.
- I'm asked to confirm before a release is deleted.
- If something is wrong with what I entered, I'm told what and where.

## Stack

| Part | Tech |
|---|---|
| API | .NET 10, ASP.NET Core Web API (controllers), Scalar for API docs |
| Data | PostgreSQL 18 (Docker), Npgsql with raw SQL. No Entity Framework, Dapper or MediatR |
| Auth | JWT bearer tokens, PBKDF2 password hashes |
| Frontend | Angular 22 (standalone, zoneless, signals), Tailwind CSS 4 |
| Tests | xUnit + Moq, Testcontainers (real Postgres), WebApplicationFactory; Vitest for the frontend |

## Running it

You need the .NET 10 SDK, Docker, and Node 24.15+ (Angular 22's minimum).

```bash
# 1. Database
docker compose up -d

# 2. API  →  http://localhost:5080   (API docs: http://localhost:5080/scalar/v1)
cd backend/src/Api
dotnet run

# 3. Frontend  →  http://localhost:4200
cd frontend
npm install
npm start
```

On startup the API creates the tables if they don't exist. In Development it also seeds:

- the demo user **admin / admin** (also shown on the login page)
- 14 releases, enough for two pages

Postgres runs on port **5450** so it doesn't clash with a local install. Connection string and the dev JWT secret are in `backend/src/Api/appsettings.Development.json`.

## Running the tests

```bash
# Backend: unit + integration (integration tests start their own Postgres container, so Docker must be running)
cd backend
dotnet test

# Frontend
cd frontend
npm test -- --watch=false
```

## API

Only register and login are public; everything else needs `Authorization: Bearer <token>`.

| Method | Route | Auth | Result |
|---|---|---|---|
| POST | `/api/auth/register` | public | 201 user · 400 invalid · 409 username taken |
| POST | `/api/auth/login` | public | 200 `{ userId, username, token, expiresAt }` · 401 |
| GET | `/api/auth/me` | token | 200 current user |
| GET | `/api/releases?search=&page=1&pageSize=10` | token | 200 page of releases, newest first |
| GET | `/api/releases/{id}` | token | 200 · 404 |
| POST | `/api/releases` | token | 201 with `Location` · 400 |
| PUT | `/api/releases/{id}` | token | 200 · 400 · 404 |
| DELETE | `/api/releases/{id}` | token | 204 · 404 |

Errors are [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457). Validation errors list every broken rule at once, keyed by field:

```json
{
  "status": 400,
  "title": "One or more validation errors occurred.",
  "errors": {
    "title": ["Title is required."],
    "releaseYear": ["Release year must be between 1900 and 2027."]
  }
}
```

To try the protected endpoints in Scalar: call login, copy the token, paste it in the Bearer field.

## Project layout

```
docker-compose.yml        Postgres
backend/
  src/
    Domain/               Release, ReleaseFormat, User. No dependencies
    Application/          business rules: ReleaseService, UserService, the ports they need
    Infrastructure/       SQL repositories, schema script, seeding, password hashing, JWT
    Api/                  controllers, error mapping, auth setup, Program.cs
  tests/
    UnitTests/            services (with Moq), password hasher, token generator
    IntegrationTests/     repositories and HTTP endpoints against a real Postgres
frontend/
  src/app/
    auth/                 login, register, AuthService, interceptor, guards
    releases/             list page, one form for new + edit, API client
    shared/               pagination, confirm dialog, error box
    core/                 error helpers
```

The design decisions and trade-offs are in [ARCHITECTURE.md](ARCHITECTURE.md).

## Known limitations

- No refresh tokens: the session lasts 60 minutes, then you log in again.
- No user management beyond register/login (no roles, no password change).
- One shared catalog: every logged-in user sees and edits the same releases.
- No optimistic concurrency: if two people edit the same release, the last save wins.
