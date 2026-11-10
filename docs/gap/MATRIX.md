# MATRIX.md — AZOA vs Web4 (Field Guide cutover)

**Fork:** https://github.com/maxgershfield/azoa-autonomous-zones-of-action  
**Branch:** `field-guide-azoa-cutover`  
**Local:** `_compare_azoa/` (`origin`=Max, `upstream`=Jade)  
**Guardrails:** [Docs/AZOA_FORK_GUARDRAILS.md](../AZOA_FORK_GUARDRAILS.md)

| Capability | Web4 source | AZOA today | Gap | Priority | Port style |
|------------|-------------|------------|-----|----------|------------|
| Avatar authenticate/register | ONODE `/api/avatar/*`, hitchhikers-auth | `/api/user/login|register` | Wrong path/shape; no JWT `id` | **P0** | Compat facade |
| Google GIS idToken | `/api/avatar/auth/google` | `/auth/oauth/*` redirect | Protocol mismatch | **P0** | Compat facade (GIS validate) |
| Refresh / revoke token | avatar refresh/revoke | AuthNotBefore logout only | No refresh store | **P0** | Compat facade + opaque refresh |
| get-logged-in-avatar | GET avatar | GET `/api/user/{id}` | No me-from-bearer | **P0** | Compat facade |
| JWT claim `id` | OASIS mint | claim `sub` only | hitchhikers breaks | **P0** | Mint `id`+`sub` |
| Holon notes/places/events | `/api/data/save-holon` etc | `/api/holon` | Shape/metadata query | **P0** | Compat facade or FG rewrite |
| Wallet ensure + read | keys + wallet APIs | `/api/wallet` | Partial shape | **P0** | Adapter / client map |
| NFT list + mint | `/api/nft/*` | `/api/nft`, `/api/usernft` | Partial | **P0** | Adapter |
| Quest start/progress/complete | STAR Quests | Actions/runs | No pin-title progress | **P0** | New Action contract or STAR-compat |
| Geo nearby / place-pin | STAR GeoNFTs | none | Missing | **P0** | New AZOA POI feature |
| Trust Graph UI id | avatar GUID | User.Id | Bind only | **P0** | keep client-side + same GUID |
| Trust Graph HTTP | `/api/trustgraph/*` | none | Missing | **P1** | New AZOA feature later |
| FG MCP `field_guide_*` | finsbury-guide MCP | platform MCP only | Product tools Missing | **P1** | Keep FG MCP as BFF |
| Photo upload | `/api/files/upload` | none | Missing | **P1** | New AZOA feature |
| GeoNFT mint-and-place | ONODE+STAR | mint only | Missing | **P1** | New feature |
| Portability | `/api/data/portability` | none | Missing | **P2** | Later / Never HyperDrive |
| Karma | KarmaController | excluded | — | Never | Never |
| HyperDrive / HoloNET / MEF | providers | excluded | — | Never | Never |
| STAR CLI / STARNET | STAR ODK | azoa-node-cli | — | P2 | Later |
| Ceremony Dual | CeremonyController | KYC/credentials | — | P2 | Never as OASIS |
| Braid | HolonicBraid | none | — | Never | Never |
| Launchboard CMTAT | Launchboard adapter | parked | — | Later | One-pager later |

## Backlog tickets

1. **P0-auth** — `/api/avatar/*` compat + JWT `id` + refresh (this sprint)
2. **P0-clients** — point hitchhikers-auth / FG web / FG MCP at fork staging
3. **P0-tg-id** — document User.Id ≡ avatarId for Trust Graph
4. **P1-holon-wallet** — data/wallet facades or FG client remap
5. **P1-quest-geo** — Action progress + POI nearby
6. **P2** — TG HTTP, photos, portability

## Agent artifacts

- [AUTH.md](AUTH.md)
- [PRODUCT_APIS.md](PRODUCT_APIS.md)
- [WEB4_KEEPERS.md](WEB4_KEEPERS.md)
- [IP.md](IP.md)
