# Architecture

## Layers

```
backend/src/
  Domain/          Release, ReleaseFormat, User. Plain classes, no dependencies.
  Application/     ReleaseService and UserService (all business rules and validation),
                   the ports they need (repositories, password hasher, token generator), DTOs.
  Infrastructure/  Npgsql repositories, schema script, seeding, PBKDF2 hashing, JWT creation.
  Api/             controllers, ProblemDetails mapping, JWT validation, DI wiring.
```

Dependencies point inward: Api → Application → Domain, and Infrastructure implements the Application interfaces. The Application project doesn't reference Npgsql, ASP.NET or any JWT library, so the business rules can be tested with plain mocks and the storage could be swapped without touching them.

Controllers are thin: they take the request, call one service method and return the result. No try/catch in them (see *Errors* below).

## Request flow

Creating a release:

1. `ReleasesController.Create` receives the JSON. `[ApiController]` rejects malformed JSON (e.g. an unknown format like `"Cassette"`) with a 400 before our code runs.
2. `ReleaseService.Create` validates every rule and collects **all** the failures, so the user sees everything that's wrong at once. Then it trims the text, sets the id and timestamps and calls the repository.
3. `ReleaseRepository.Add` runs a parameterized `INSERT`.
4. The controller returns 201 with a `Location` header pointing to the new release.

## Data access without an ORM

The spec rules out Entity Framework, Dapper and MediatR, so the repositories use Npgsql directly:

- **One `NpgsqlDataSource`** (registered with `AddNpgsqlDataSource`) handles connection pooling. Each repository method creates a command from it, so there's no connection management in the code.
- **Always parameters**, never string concatenation for values. The only interpolation is the column list and the optional `WHERE`, both constants.
- **Schema as one idempotent script** (`Persistence/Scripts/schema.sql`, embedded in the assembly) that runs on every startup. Without an ORM there's no migrations tool; for two tables, `CREATE TABLE IF NOT EXISTS` is enough. A real product would add a migration tool like DbUp.
- **Search and count in one round trip.** The list needs the page and the total count, so `Search` sends both queries in one `NpgsqlBatch`.
- **Search is case-insensitive** (`ILIKE`), and `%` and `_` typed by the user are escaped, so searching for "100%" finds the text "100%".
- **Usernames are unique regardless of case** with a unique index on `lower(username)`. The service checks first to give a friendly message; if two registrations race past that check, the index rejects the second one and the repository turns the unique-violation into the same `ConflictException`.
- **Ids are generated in the app** (`Guid.CreateVersion7()`): time-ordered, so they index well, and known before the insert.
- **Enums are stored as text** (`'Single'`, `'EP'`, `'Album'`) with a `CHECK` constraint, so the table is readable on its own.

## Authentication

- `POST /api/auth/register` and `POST /api/auth/login` are the only public endpoints. `ApiBase` has `[Authorize]`, so every other endpoint needs a token unless it opts out with `[AllowAnonymous]`.
- **Passwords** are hashed with ASP.NET Core Identity's `PasswordHasher` (PBKDF2, salted). I used just the hasher, not the rest of Identity, because Identity's default user store is built on Entity Framework.
- **Tokens** are HS256 JWTs carrying `sub` (user id) and `unique_name` (username), valid for 60 minutes. The same `JwtOptions` are used to create them (Infrastructure) and to validate them (the built-in JwtBearer handler in Api). Issuer, audience, signature and lifetime are all validated, with no clock skew.
- The app refuses to start if the JWT secret is shorter than 32 characters.
- **Login never says which part was wrong**: unknown username and wrong password both return the same 401, so the endpoint can't be used to find out which usernames exist.

## Errors

The business layer throws three exceptions and `ApiExceptionHandler` maps them, so controllers stay free of try/catch:

| Exception | HTTP | When |
|---|---|---|
| `ValidationException` | 400 with field errors | a business rule failed |
| `NotFoundException` | 404 | the release doesn't exist (or was deleted meanwhile) |
| `ConflictException` | 409 | the username is taken |

Anything else becomes a generic 500 without leaking the exception message.

On the frontend:

- Simple rules (required, lengths, year range) are checked in the browser and shown under each field.
- Anything that only the server can know (username taken, wrong password, a release deleted by someone else, the API being down) comes back on submit and is shown in **one error box** at the top of the form.

## Frontend

- **Standalone components, signals, zoneless.** No NgRx: `AuthService` keeps the session in a signal, and each page keeps its own loading/error state in signals.
- **Auth:** an interceptor adds the token to API calls and logs the user out on a 401 (expired or rejected token). Guards keep anonymous users out of the catalog and logged-in users out of login/register.
- **List state lives in the URL** (`?search=…&page=2&size=20`), so refresh, back and shared links keep the same view. Search waits 300 ms after typing stops and goes back to page 1.
- **One form component** for both `/releases/new` and `/releases/:id/edit`; the route's `id` is bound as an input.
- **Tailwind:** utility classes in the templates, plus a few shared classes (`.input`, `.btn-primary`…) in `styles.css` for the pieces every form repeats.

## Testing

The backend was built test-first, one feature at a time (the commit history shows each test commit before its implementation):

- **Unit tests** for `ReleaseService` and `UserService` with Moq: every validation rule and its boundaries (e.g. 1900 and next year accepted, 1899 and the year after rejected), not-found handling, hashing before saving, login failures. `TimeProvider` is mocked so "next year" is deterministic.
- **Unit tests** for the password hasher (salted, verifies) and the token generator (valid signature, claims, expiry).
- **Repository tests** against a real Postgres started by Testcontainers, using the real schema script. Mocking the database would hide exactly the mistakes hand-written SQL can have.
- **Endpoint tests** with `WebApplicationFactory`: status codes, auth on every endpoint, field errors over HTTP, the `Location` header, enum-as-text JSON, search and paging.
- **Frontend tests** (Vitest): the interceptor, the guards, the pagination component and the release form.

One bug the endpoint tests caught: .NET timestamps have 100 ns precision and Postgres stores microseconds, so the `createdAt` returned by a POST didn't match the one a later GET returned. The services now round "now" to microseconds, and there's a unit test for it.

## Trade-offs

- **Token in `localStorage`.** It survives a page refresh, which a demo needs, but a script injected into the page (XSS) could read it. An HttpOnly cookie avoids that but needs CSRF protection. Angular escapes template output by default, which lowers the XSS risk.
- **No refresh tokens.** Shorter code; the user logs in again after 60 minutes.
- **Validation rules are written twice** for releases (frontend validators and `ReleaseService`). The frontend copy is only for quick feedback; the backend is the one that decides.
- **Offset paging** (`LIMIT/OFFSET`). Simple and fine for a catalog this size; very large tables would want keyset paging.
- **Schema script instead of migrations.** Fine for a fixed two-table schema; schema changes over time would need a migration tool.
- **Last write wins** on concurrent edits. A `version` column checked in the `UPDATE` would add optimistic concurrency.
