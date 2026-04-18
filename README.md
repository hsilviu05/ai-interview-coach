# ai-interview-coach

## Toolchain

This repo is pinned to:

- `.NET SDK 10.0.103` in `global.json`
- `Node.js 22.20.0` in `.nvmrc`
- `npm 10.9.3` in `frontend/package.json`

Use those exact versions for local development and CI. The frontend also disables Angular's persistent disk cache in `frontend/angular.json` because the local cache path was causing native `node` crashes during `ng build` on macOS.

## Clean local setup

1. Install the pinned SDKs.
2. Restore backend packages:

```bash
dotnet restore backend/AIInterviewCoach.Tests/AIInterviewCoach.Tests.csproj
```

3. Install frontend packages:

```bash
cd frontend
npm ci
```

## Backend JWT secret

The API no longer stores a JWT signing key in source control. Set `Jwt:Key` locally before starting the backend.

Using `dotnet user-secrets`:

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-a-random-32-byte-secret" --project backend/AIInterviewCoach.API
```

Using an environment variable:

```bash
export Jwt__Key="replace-with-a-random-32-byte-secret"
```

Or use an ignored local config file:

`backend/AIInterviewCoach.API/appsettings.Development.Local.json`

```json
{
  "Jwt": {
    "Key": "replace-with-a-random-32-byte-secret"
  }
}
```

The issuer and audience still come from `backend/AIInterviewCoach.API/appsettings.json`.

## Reproducible verification

Run these from a clean checkout after `npm ci` and `dotnet restore`:

```bash
dotnet test backend/AIInterviewCoach.Tests/AIInterviewCoach.Tests.csproj
cd frontend && npm run build
cd frontend && npx ng test --watch=false
cd frontend && npx playwright test
```
