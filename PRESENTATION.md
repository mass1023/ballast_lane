# Presentation

Everything is in this repo. The full-stack app is in `backend/` and `frontend/` (see the [README](README.md) and [ARCHITECTURE.md](ARCHITECTURE.md)) and the GenAI exercise is in [`GenAI/`](GenAI/README.md).

I used Claude Code for both parts. Here I try to explain how I went through the exercise, the decisions I made and the things I had to correct along the way.

## Starting point

The spec doesn't allow EF or Dapper, which was fine for me. At AlphaPoint all the data access was raw SQL on MySQL with no ORM, so writing the queries by hand is something I'm used to. Here it's Postgres with Npgsql instead. The rest of the setup is how I usually work on .NET projects: the layers, Postgres in docker-compose and testing against a real database with Testcontainers.

## The user story

I went with a record label catalog:

> As the label's catalog admin, I want to log in and add, edit and remove releases (title, artist, release year, format) so that our catalog stays up to date.

I had a short amount of time so I kept the scope small. One table for releases and one for users. The format is a fixed list. There's a register page, login and logout, but no user management or roles, and everybody sees the same catalog. There's a seeded admin/admin account and the credentials are shown on the login page so it's easy to try.

Only register and login are public, all the release endpoints need a token. That way it's clear which endpoints are authorized and which ones aren't.

The rules are simple: title and artist are required (max 200), the year has to be between 1900 and next year, the username is 3 to 50 characters and can't be repeated (also ignoring upper/lower case), and the password needs at least 5 characters.

## Backend

I used Npgsql directly with SQL. All the values go as parameters. The search and the total count go in one round trip, and the unique username is an index on `lower(username)`. The schema is one SQL script that runs when the API starts.

The layers are Domain, Application, Infrastructure and Api. The business rules are in Application, in `ReleaseService` and `UserService`, and they don't know anything about Postgres or JWT, so they're easy to test with mocks. When a rule fails the service throws an exception (validation, not found or conflict) and one handler in the API turns it into a 400, 404 or 409. Because of that the controllers are basically one line per action.

For auth I used JWT with the JwtBearer handler that comes with ASP.NET. For the passwords I used the PBKDF2 hasher from Identity.

## Frontend

Angular because it's what I know best. I used Angular 22 with signals and standalone components, and Tailwind for the styles. I made the pagination and the delete confirmation myself.

For validation I wanted something simple. The basic rules are checked in the browser and show under each field. Things only the server knows, like a username already taken or a wrong password, come back on submit and show in one error box at the top.

## TDD

I did the backend one piece at a time, first the tests and then the code. You can see it in the commits.

The unit tests check every rule and its limits. The repository and endpoint tests run against a real Postgres in Docker, since mocking the database would hide the SQL bugs.

## GenAI exercise

The details are in [GenAI/README.md](GenAI/README.md).

In the prompt I set the stack and the scope. The AI asked me two things: if `responsable_id` was required, with a 400 when the user doesn't exist, and if `due_date` was required. The rest it decided by itself and I reviewed it.

The integration tests caught two `DateOnly` bugs between Dapper and Npgsql that mocks wouldn't have caught.

When I reviewed it I found things I'd ask differently next time: there's no authentication, "today" is calculated in UTC, an update can lose a race against a delete, there's no paging, and `responsable` ended up in an English API. The improved prompt is at the end of that README.

## With more time

- Refresh tokens, or a cookie instead of keeping the token in `localStorage`
- Handling two people editing the same release at the same time
- End-to-end tests for the main flows
- Auth and the other fixes for the GenAI API
