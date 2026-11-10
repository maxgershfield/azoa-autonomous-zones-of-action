# AZOA fork staging for Field Guide

**Repo:** https://github.com/maxgershfield/azoa-autonomous-zones-of-action  
**Branch:** `field-guide-azoa-cutover`  
**Upstream:** JadeZaher/azoa-autonomous-zones-of-action

## Railway (recommended)

1. Create a Railway project linked to `maxgershfield/azoa-autonomous-zones-of-action`.
2. Deploy branch `field-guide-azoa-cutover` (or merge to a `staging` branch).
3. Add SurrealDB service (see `docs/NODE-HOST.md` and `deploy/railway/`).
4. Set at least:
   - `Jwt__Key` (32+ chars)
   - `Jwt__Issuer` / `Jwt__Audience`
   - `SurrealDb__*` / `SurrealRuntime__*` per NODE-HOST
   - `AvatarCompat__GoogleClientId` = Field Guide GIS client id
   - Self-registration enabled for staging (`SelfRegistration` / host policy per AZOA docs)
5. Public HTTPS URL becomes `AZOA_BASE_URL` for hitchhikers-auth and Field Guide.

Root `railway.json` is already present. Prefer the documented Surreal + WebAPI composition over bare `railway up` from an unrelated monorepo root.

## Local smoke (before Railway)

```bash
./dev-up.sh --no-build   # or full rebuild
curl -s http://127.0.0.1:5000/health || curl -s http://127.0.0.1:5000/swagger/index.html
# then CLIENT_POINTING.md curl register/authenticate
```

## Guardrails

No OASIS `.cs` copies. Avatar routes are a thin facade over `UserManager`.
