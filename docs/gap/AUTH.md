# AUTH.md — hitchhikers-auth / MCP OAuth vs AZOA

## hitchhikers-auth routes (must have)

| Method | Path | Body | Success needs |
|--------|------|------|---------------|
| POST | `/api/avatar/authenticate` | `Username`, `Password` | `result.{id,username,email,jwtToken,refreshToken?,level?}` |
| POST | `/api/avatar/register` | `AcceptTerms`, `AvatarType`, `ConfirmPassword`, `Email`, `FirstName`, `LastName`, `Password`, `Username` | not isError (no JWT required) |
| POST | `/api/avatar/auth/google` | `idToken` | same auth record as authenticate |
| POST | `/api/avatar/refresh-token` | `refreshToken` | new jwtToken + refreshToken |
| POST | `/api/avatar/revoke-token` | Bearer + `{ token }` | not isError |
| GET | `/api/avatar/get-logged-in-avatar` | Bearer | avatar object in result |

**JWT invariant:** claim `id` = avatar/user GUID. hitchhikers-auth reads only `id`.

## MCP oauth.ts

- Wraps OASIS/AZOA JWT in opaque `fgat_` / `fgrt_` tokens locally.
- Needs underlying JWT with resolvable avatar GUID (`id` or `sub` GUID) and `exp`.
- Works once avatar facade mints correct JWT; no AZOA change for wrapping itself.

## AZOA today

- `/api/user/register`, `/api/user/login` (login returns raw JWT string in `Result`)
- JWT claim `sub` = user Id; **no claim `id`**
- Google via `/auth/oauth/*` redirect + exchange (not GIS idToken POST)
- No refresh-token store/API; logout = AuthNotBefore watermark
- No `/api/avatar/*`

## Gap

| Capability | Status |
|------------|--------|
| `/api/avatar/*` routes | Missing |
| JWT claim `id` | Missing |
| Auth result object with jwtToken+profile | Missing |
| Username login | Partial (Email/UserId only) |
| Google idToken POST | Missing |
| refresh-token / revoke-token | Missing / Partial |
| get-logged-in-avatar | Missing |

## Compat facade map

Implement thin `/api/avatar/*` on AZOA calling UserManager + UserLoginTokenIssuer.
Mint JWT with both `id` and `sub` = `user.Id`. Option B: opaque refresh tokens at login.
