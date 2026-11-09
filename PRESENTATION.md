# Presentation: thought process

This repo holds both parts of the exercise:

| Part | Where | Details |
|---|---|---|
| Full-stack app (record label catalog) | `backend/`, `frontend/` | [README](README.md) · [ARCHITECTURE](ARCHITECTURE.md) |
| GenAI exercise (task management API) | `GenAI/` | [GenAI README](GenAI/README.md) |

I built both with Claude Code as a pair programmer. Below is how I approached it, what I decided and why, and where I had to correct the AI.

---

## 1. Reading the spec

I broke the spec into requirements first:

- **Backend:** a data table plus a users table, CRUD endpoints, a second API for user creation and login with authorized and non-authorized endpoints, separate data and business layers, unit tests for every layer, TDD preferred. **No Entity Framework, Dapper or MediatR.**
- **Frontend:** any framework, responsive, CRUD for the use case, clean structure.
- **Delivery:** a README, seeded data and credentials, everything in one public repo.
- **GenAI:** a separate task-management API generated with an AI tool, plus the prompt, a sample of the output, and how I validated and corrected it.

A week before, I'd done a similar take-home. My first question was whether I could reuse it. Mostly no: it was built on Entity Framework, which this spec bans, and its domain was different. I reused only the ideas that still applied: the layer split, Postgres in docker-compose, and running the tests against a real database with Testcontainers.

## 2. User story and scope

> As the label's catalog admin, I want to log in and add, edit and remove releases (title, artist, release year, format) so that our catalog stays up to date.

I kept the scope small on purpose, because the time was short and the grading is on quality, not features:

- **One data table** (`releases`) plus `users`.
- **Format is a fixed list** (Single, EP, Album).
- **One shared catalog**, with no roles or permissions.
- **Users can register, log in and log out**, but there's no user management. A seeded `admin / admin` account is shown on the login page.
- **Only register and login are public.** Every release endpoint needs a token, which makes "authorized vs non-authorized" easy to see.
- **Business rules:** title and artist required, max 200 characters; year between 1900 and next year; username 3–50 characters and unique regardless of case; password at least 5 characters.

## 3. Key decisions and why

| Decision | Why |
|---|---|
| **PostgreSQL with Npgsql and raw SQL** | The spec rules out EF and Dapper. Npgsql alone is the standard driver, and it let me show parameterized SQL, a batched search + count, and a unique index on `lower(username)`. |
| **Schema as one idempotent SQL script at startup** | No ORM means no migrations tool. Two tables don't need one. |
| **Clean Architecture: Domain / Application / Infrastructure / Api** | The business rules live in `Application` and don't know about Postgres, ASP.NET or JWT. That's what makes them easy to unit-test. |
| **Business errors as exceptions, mapped in one place** | The services throw validation, not-found and conflict exceptions; one handler turns them into 400/404/409 ProblemDetails. Controllers stay one line each. |
| **JWT with the built-in JwtBearer handler** | Standard and well tested. Passwords use Identity's PBKDF2 hasher, without the rest of Identity, whose stores need EF. |
| **Angular 22 + Tailwind** | Angular is what I'm most comfortable with. Signals and standalone components keep it small, and Tailwind has no component library, so I built the pagination and confirm dialog myself. |
| **Validation split** | Simple rules (required, length, year range) are checked in the browser under each field. Anything only the server knows (username taken, wrong password) comes back on submit and shows in one error box. It's simple and quick to build. |

The full list of trade-offs is in [ARCHITECTURE.md](ARCHITECTURE.md).

## 4. How I worked: TDD, one layer at a time

For each feature I committed the tests first, then the code that makes them pass. The backend git history shows it (messages shortened):

```
release business rules           → release business rules implementation
registration and login rules     → registration and login implementation
password hashing and JWT rules   → password hashing and JWT generation
Postgres integration tests       → SQL repositories
auth and release endpoint tests  → auth and release endpoints
```

- **Unit tests** cover every business rule and its boundaries (e.g. 1900 and next year are accepted, 1899 and the year after are rejected), with Moq for the repositories.
- **Repository and endpoint tests** run against a real Postgres in Docker. With hand-written SQL, mocking the database would hide exactly the bugs that matter.

## 5. Working with the AI, and where I corrected it

Claude Code wrote most of the code. My job was to make the decisions, check the output and keep the scope in check. The moments that mattered:

- **It decided too much on its own at first.** It started scaffolding after a few answers. I stopped it and made it ask first. It came back with questions I hadn't thought through: the data store, login by email or username, how to report errors, test doubles, the year range, password rules, list paging. I answered each one, and those answers are the decisions in section 3.
- **It misread the GenAI section.** It suggested the GenAI part was only a write-up and that my main app should be the task manager. I read the spec again: it's a separate API, in any language. I kept them separate.
- **The tests caught a real bug in AI-written code.** .NET timestamps are more precise than Postgres's, so a release's `createdAt` from the POST response didn't match the one a later GET returned. The endpoint test failed, and the fix (round to microseconds) got its own unit test.
- **A copied config hid code from git.** The `.gitignore` reused from my previous project had a `Releases/` rule meant for Visual Studio build folders. It also matched my `Releases` feature folders, so that code would never have been committed. It was caught before publishing.
- **It ran git actions I wanted to do myself.** It committed and later staged files on its own. I had it undo the commits, kept every git action for myself, and wrote the commits in test → code order.
- **The code had to read like mine.** I gave it a project I'd written by hand as a style reference, then went over the generated code myself: shorter names, fewer comments, my patterns. I need to be able to explain and own every line.
- **I simplified when it over-built.** The first frontend version mapped every server error onto its own field. I asked for one error box instead, which meant less code and the same clarity.
- **Environment issues.** Angular 22 needed a newer Node version than I had. I upgraded instead of falling back to an older Angular.

**What I take from it:** the AI is fast at producing code, but the quality came from three things it doesn't do on its own:
- clear decisions made up front
- tests against the real database
- reading every change before keeping it

## 6. GenAI exercise

The full write-up is in [GenAI/README.md](GenAI/README.md): the prompt, the two questions the AI asked, the decisions it made on its own, code samples, how I validated it, the two bugs the tests caught, and what I'd improve.

The short version:

- **The prompt fixed the stack** (.NET 10, Postgres, Dapper, DDD, TDD) and **the scope** (read-only users, full task CRUD, hard delete, listed statuses).
- **The AI asked two questions:** is `responsable_id` required (and 400 if the user doesn't exist), and is `due_date` required. It made the remaining decisions itself, and I reviewed each one.
- **The integration tests caught two Dapper/Npgsql bugs** with `DateOnly` that mocked tests would have missed.
- **My review found gaps I'd close with a better prompt:**
  - no authentication or `created_by`
  - "today" computed in UTC, which rejects valid same-day dates in the evening in my timezone
  - an update can lose a race with a delete
  - a Spanish field name (`responsable`) in an English API: names in a prompt become the API contract
  - no paging
- **The improved prompt** is at the end of that README.

## 7. If I had more time

- Refresh tokens, or HttpOnly cookies instead of `localStorage` for the token.
- Optimistic concurrency on edits (a version column).
- Keyset paging for large catalogs.
- End-to-end tests (Playwright) for the main flows.
- Authentication and the other fixes listed for the GenAI API.
