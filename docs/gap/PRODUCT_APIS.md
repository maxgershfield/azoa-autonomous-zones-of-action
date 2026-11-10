# PRODUCT_APIS.md — Field Guide / Trust Graph vs AZOA

## P0 (cutover blockers)

1. Auth bridge (`/api/avatar/*` + JWT `id`) — shipped
2. Holon CRUD (`/api/data/*` facade) — shipped
3. Wallet ensure + read + NFT list — shipped (ledger mint)
4. Quest start/progress/complete (STAR-compat) — shipped
5. Check-in path end-to-end — ready for staging smoke
6. Geo nearby / place-pin (STAR-compat) — shipped

See [STAR_NFT_HOLON_ADAPTERS.md](STAR_NFT_HOLON_ADAPTERS.md).

## P1

- Quest publish; glove/sit mint; shared drop geo place
- Photo upload; Trust Graph bootstrap/state/card
- Guide holon CRUD; keep FG MCP as BFF on AZOA HTTP

## P2 / N/A

- Portability; TG POI; membership Stripe; partner/merchant flows
- Client-local: quest packs, Trust Graph UI projections, map scrapes

## AZOA primitives that exist (wrong nouns/routes)

User, Wallet, Holon, NFT, Action, platform MCP tools (`user_scoped_query`, `holon_traverse`, …).
**No** `field_guide_*` tools, Trust Graph routes, GeoNFT, or OASIS `/api/data/*`.
