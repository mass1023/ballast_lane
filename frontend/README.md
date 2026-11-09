# Record Label Catalog: frontend

Angular 22 (standalone, zoneless, signals) + Tailwind CSS 4. It talks to the API in `../backend`; see the [root README](../README.md) for running the whole app.

## Commands

You need Node 24.15+ and the API running on `http://localhost:5080`.

```bash
npm install
npm start                    # http://localhost:4200
npm test -- --watch=false    # unit tests (Vitest)
npm run build                # production build in dist/
```

Log in with the demo account **admin / admin**, or create one from the register page.

The API URL is in `src/environments/environment.ts`.

## Structure

```
src/app/
  auth/        login and register pages, AuthService (session in a signal + localStorage),
               interceptor (adds the token, logs out on 401), guards
  releases/    list page (search, paging, delete), one form for new + edit, ReleasesApi
  shared/      pagination, confirm dialog (native <dialog>), error box
  core/        turns API errors into messages
  shell.ts     header with the user and log out, wraps the logged-in pages
```

## Routes

| Route | Page | Access |
|---|---|---|
| `/login` | log in | logged out only |
| `/register` | create account, then logged in automatically | logged out only |
| `/releases` | catalog list; search and page live in the URL (`?search=…&page=2&size=20`) | logged in |
| `/releases/new` | new release | logged in |
| `/releases/:id/edit` | edit release (same form component) | logged in |

## Validation

- Simple rules (required, lengths, year range) are checked in the browser and shown under each field.
- Errors only the server knows (username taken, wrong password, release not found, API down) come back on submit and are shown in one error box at the top of the form.

## Styling

Tailwind utility classes in the templates. The pieces every form repeats (`.input`, `.label`, `.btn-primary`, `.btn-secondary`, `.btn-danger`…) are defined once in `src/styles.css`.
