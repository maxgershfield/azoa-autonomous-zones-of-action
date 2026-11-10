# Point hitchhikers-auth / Field Guide / MCP at AZOA fork staging

## Env vars

| Consumer | Variable | Example |
|----------|----------|---------|
| hitchhikers-auth | `AZOA_BASE_URL` (or `OASIS_BASE_URL`) | `https://azoa-fg-staging.up.railway.app` |
| field-guide-web4-client | `FIELD_GUIDE_AZOA_BASE_URL` or `VITE_AZOA_BASE_URL` | same |
| finsbury-guide MCP | `AZOA_BASE_URL` / `OASIS_BASE_URL` (passed into client config) | same |
| AZOA Google GIS | `AvatarCompat__GoogleClientId` | Field Guide Google OAuth client id |

Until staging is live, defaults remain the legacy ONODE
`https://motivated-reflection-production-457c.up.railway.app`.

## Verify login (local or staging)

```bash
# against a running AZOA node with self-registration enabled
export AZOA_BASE_URL=http://127.0.0.1:5000
cd hitchhikers-auth && npm test   # if suite covers auth
# or manual:
curl -s -X POST "$AZOA_BASE_URL/api/avatar/register" \
  -H 'Content-Type: application/json' \
  -d '{"AcceptTerms":true,"AvatarType":"User","ConfirmPassword":"password1","Email":"fg@example.com","FirstName":"F","LastName":"G","Password":"password1","Username":"fguser"}'
curl -s -X POST "$AZOA_BASE_URL/api/avatar/authenticate" \
  -H 'Content-Type: application/json' \
  -d '{"Username":"fguser","Password":"password1"}'
```

Expect `result.jwtToken` and `result.id`. Decode JWT and confirm claim `id` equals `result.id`.

## Trust Graph binding

Field Guide Trust Graph UI and hitchhikers-auth both treat **avatar id = JWT claim `id`**.
On AZOA that value is `User.Id`. No separate mapping table. See [TG_AVATAR_BINDING.md](TG_AVATAR_BINDING.md).
