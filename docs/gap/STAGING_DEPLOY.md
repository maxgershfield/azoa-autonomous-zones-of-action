# AZOA fork staging for Field Guide

**Fork:** https://github.com/maxgershfield/azoa-autonomous-zones-of-action  
**Branch:** `field-guide-azoa-cutover`  
**Railway project:** `azoa-fg-staging` (`d8b733d9-2421-4f1a-a79a-18ceec962741`)  
**API URL:** https://azoa-api-production-56786d11.up.railway.app  

## Services

| Service | Status notes |
|---------|----------------|
| `surrealdb` | Image `surrealdb/surrealdb:v3.1.4` + volume `/data`. **Must** run with start args: `start --log info --user root --pass root rocksdb:///data/db` (wrapper: `deploy/railway/Dockerfile.surrealdb`). Without this, the container prints help and exits. |
| `azoa-api` | GitHub repo `maxgershfield/azoa-autonomous-zones-of-action` @ `field-guide-azoa-cutover`. Domain on port 8080. |

## Required API variables

- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- `ASPNETCORE_ENVIRONMENT=Development` (allows boot-time schema for staging)
- `SURREALFORGE_URL=http://surrealdb.railway.internal:8000` (+ NS/DB/USER/PASS)
- `SurrealRuntime__Endpoint=http://surrealdb.railway.internal:8000` (+ matching NS/DB/user/pass)
- `ASPNETCORE_URLS=http://0.0.0.0:8080` (or honor `$PORT`)
- `AvatarCompat__GoogleClientId` = Field Guide GIS client id

## Client pointing

```bash
export AZOA_BASE_URL=https://azoa-api-production-56786d11.up.railway.app
```

See OASIS tree `Docs/AZOA_GAP/CLIENT_POINTING.md`.

## Local smoke (always available)

```bash
./dev-up.sh
export AZOA_BASE_URL=http://127.0.0.1:5000
# then register/authenticate curls from CLIENT_POINTING.md
```

## Guardrails

No OASIS `.cs` copies. Avatar routes are a thin facade over `UserManager`.
