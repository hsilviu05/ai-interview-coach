# ai-interview-coach

## Local setup

### Backend JWT secret

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

The issuer and audience still come from [backend/AIInterviewCoach.API/appsettings.json](/Users/silviu/ai-interview-coach/backend/AIInterviewCoach.API/appsettings.json).
