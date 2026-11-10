# PRODUCT_APIS.md — Field Guide / Trust Graph vs AZOA

## P0 (cutover blockers)

1. Auth bridge (`/api/avatar/*` + JWT `id`)
2. Holon CRUD used by notes/places/events (facade or client rewrite of `/api/data/*`)
3. Wallet ensure + read + NFT list
4. Quest start/progress/complete (Action semantics or STAR-compat)
5. Check-in path end-to-end
6. Geo nearby (or substitute) for map layer

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
