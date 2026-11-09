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

---

## 1. The prompt

```
Create a folder named "GenAI" with a RESTful API for a task management system.

Use .NET 10 Web API (controllers), PostgreSQL in docker-compose with Dapper. Use DDD for the
design pattern and include a test project using TDD.

The system will have 2 entities:
- Users: simple table (id, username, created_at) with a read-only repository, no user CRUD.
- Tasks: id, title, description, status (pending, in progress and done), due_date, and
  responsable_id. Tasks require full CRUD functionality with hard delete.
```

What I put in it on purpose:

- **The stack is fixed** (framework, database, data access, design approach), so the AI doesn't pick for me and I don't have to undo its choices.
- **The status values are listed**, so it doesn't invent its own.
- **Scope limits are explicit**: users are read-only with no CRUD, and delete is a hard delete. That keeps it from over-building (soft deletes, user management).
- **TDD is part of the request**, so the tests come with the code instead of after it.

## 2. What the AI asked before writing code

Claude Code asked two questions instead of guessing:

| Question | My answer |
|---|---|
| Is `responsable_id` required, and should the API return 400 if the user doesn't exist? | Yes, required, and 400 if the id isn't an existing user. |
| Is `due_date` required? | Yes. |

## 3. Decisions the AI made on its own (reviewed)

These weren't in the prompt or my answers. I checked each one and kept it:

- **No past due dates on create**: `due_date` can't be in the past when creating a task. Updates may keep a past date, so an overdue task can still be edited.
- **New tasks always start as `Pending`**: POST doesn't accept a status. PUT is a full replace, status included.
- **Seeded users plus read-only endpoints** (`GET /api/users`), so there are valid ids to assign tasks to. Without a user CRUD, a task could never be created otherwise.
- **List filters** by status and responsible user, and no paging.
- **Field limits**: title required, max 200 characters; description optional, max 2000.
- **`due_date` is a date, not a timestamp** (`DateOnly` in C#, `DATE` in Postgres).
- **Validation lives in the domain entity** (`TaskItem.Create` / `Update`), which fits the DDD request. The "user exists" check sits in the application service, because it needs the repository.

## 4. Sample of the output

The domain entity owns its rules and reports every broken rule at once:

```csharp
public static TaskItem Create(string? title, string? description, DateOnly? dueDate, Guid responsableId, DateOnly today)
{
    var errors = Validate(title, description, TaskItemStatus.Pending, dueDate, responsableId);

    if (dueDate is not null && dueDate < today)
        errors.Add("dueDate", ["Due date cannot be in the past."]);

    ThrowIfAny(errors);

    return new TaskItem(Guid.CreateVersion7(), title!.Trim(), Normalize(description), TaskItemStatus.Pending, dueDate!.Value, responsableId);
}
```

The application service adds the check that needs the database:

```csharp
public async Task<TaskDto> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
{
    var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
    var task = TaskItem.Create(request.Title, request.Description, request.DueDate, request.ResponsableId ?? Guid.Empty, today);

    await EnsureUserExists(task.ResponsableId, ct);
    await _tasks.AddAsync(task, ct);

    return TaskDto.From(task);
}
```

An endpoint test, written before the endpoint:

```csharp
[Fact]
public async Task Create_WithUnknownResponsable_Returns400()
{
    var response = await Client.PostAsJsonAsync("/api/tasks", NewTask("Orphan", Guid.NewGuid()));

    Assert.Contains("responsableId", await ValidationErrors(response));
}
```

The full code is in `src/`, and the 47 tests are in `tests/`.

## 5. How I validated the AI's suggestions

- **Ran it:** `dotnet build`, then `dotnet test`. The integration tests run against a real Postgres (Testcontainers), not mocks.
- **Followed the TDD order:** for each layer the test came first and failed (red), then the code made it pass.
- **Smoke-tested it by hand** against the docker-compose database: listed users, created, filtered and deleted a task, opened Scalar.
- **Checked the scope against the prompt:**
  - there's no user CRUD (`POST /api/users` returns 405, and a test covers it)
  - delete is a real `DELETE`
  - the `users` foreign key stops orphaned tasks
- **Read the SQL:** every value is a Dapper parameter. Even the optional filters are parameterized (`@Status::text IS NULL OR status = @Status`) instead of building the `WHERE` from strings.
- **Read the generated code:** the review below is the result.

## 6. What was corrected

Two bugs in the first version, both caught by the integration tests while the code was being generated:

1. **`DATE` columns came back as `DateOnly`, not `DateTime`.** The first repository assumed Npgsql returns `DateTime` for `DATE`. Npgsql 10 returns `DateOnly`, so every read failed with `Error parsing column 4`. The round-trip repository test caught it.
2. **Dapper can't bind `DateOnly` parameters.** After switching to `DateOnly`, inserts failed with `The member DueDate of type System.DateOnly cannot be used as a parameter value`. The fix was a small type handler registered with Dapper:

```csharp
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to DateOnly.")
    };
}
```

Both only show up against a real database. A test suite that mocked the repositories would have passed with them.

## 7. Edge cases, authentication and validation

**Validation**
- Every broken field rule is returned at once as a 400 with field errors, not one at a time.
- An unknown `responsableId` gives a 400 on that field, and the foreign key backs it up in the database.
- A missing task gives a 404 on GET, PUT and DELETE.
- An unknown status value in the JSON is rejected with a 400.
- Status is stored as text with a `CHECK` constraint.

**Edge cases covered by tests**
- A title over 200 characters and a description over 2000 are rejected.
- A blank description is saved as `null`.
- A past due date is rejected on create but kept on update.
- Deleting twice gives 204, then 404.

**Authentication: not implemented.** I left it out of the prompt to keep the exercise on the CRUD. Every endpoint is public, and nothing records who created a task. The main project in this repo shows how I handle it (JWT, `[Authorize]` by default, user id taken from the token). See the next section for how I'd ask for it.

## 8. What I'd improve (from my review)

I didn't change the generated code. These are the issues I found and how I'd fix them, mostly by being more precise in the prompt:

1. **No authentication or `created_by`.** I'd add both to the prompt: "add a JWT login for the seeded users; `created_by` comes from the token's `sub`, never from the request body; only the creator can update or delete (403 otherwise)."
2. **"Today" is the UTC date.** In Guatemala (UTC-6), after 6 pm UTC is already tomorrow, so a task due "today" local time is rejected as in the past. Either the client sends its date, or the rule allows one day of slack.
3. **An update can lose a race with a delete.** `UpdateAsync` doesn't check how many rows the `UPDATE` touched. If the task is deleted between the read and the write, the API returns 200 with data that no longer exists. Returning the affected count (as `DeleteAsync` already does) and turning 0 into a 404 fixes it.
4. **`responsable` is a Spanish word in an English API.** It came straight from my prompt and is now in the JSON (`responsableId`). It should have been `assigneeId` or `responsibleId`. The lesson: names in a prompt become the API contract.
5. **No paging on the list.** Fine for a demo, but the list returns every task. I'd ask for `page`/`pageSize` with a total count.
6. **No `created_at` / `updated_at` on tasks.** Useful for sorting and auditing, and cheap to add.

A better prompt, with those fixed and the decisions from sections 2 and 3 written in up front:

```
Create a folder named "GenAI" with a RESTful API for a task management system.

Stack: .NET 10 Web API (controllers), PostgreSQL in docker-compose, Dapper (raw SQL).
Design: DDD layering (Domain / Application / Infrastructure / Api), test project with xUnit, TDD.

Users: table (id, username, created_at), read-only repository, no user CRUD. Seed 3 users.
Auth: JWT login for the seeded users. Every task endpoint requires a token.

Tasks: id, title, description, status, due_date, assignee_id, created_by, created_at, updated_at.
- title required, max 200; description optional, max 2000
- status: Pending | InProgress | Done, stored as text; new tasks start as Pending
- due_date: required, date only, not in the past on create (compare with the client's date)
- assignee_id: required, must be an existing user, otherwise 400
- created_by: the logged-in user from the token, never from the request body
- only the creator can update or delete (403 otherwise); hard delete
- list: filter by status and assignee, paged (page, pageSize, totalCount)

Errors as ProblemDetails with field errors; 201/204/400/401/403/404 where they apply.
Before writing code, list your assumptions and a short plan, and wait for my OK.
```
