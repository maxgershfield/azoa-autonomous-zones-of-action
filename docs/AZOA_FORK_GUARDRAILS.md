# AZOA fork guardrails (Field Guide / Trust Graph / MCP cutover)

**Fork:** https://github.com/maxgershfield/azoa-autonomous-zones-of-action  
**Upstream:** https://github.com/JadeZaher/azoa-autonomous-zones-of-action  
**Local clone:** `_compare_azoa/` (`origin` = Max fork, `upstream` = Jade)

## Goal

Run Field Guide, Trust Graph, MCP, and hitchhikers-auth against an AZOA node so product work is not blocked on David / Next Gen World–controlled OASIS tip. AZOA (Apache 2.0) is the long-term node.

## Hard rules

1. **No OASIS source copies.** Do not copy files from `OASIS Architecture/`, `ONODE/`, `STAR ODK/`, or `Providers/` into the AZOA fork. Port contracts and behavior only.
2. **Compat facade, not a second OASIS.** Where hitchhikers-auth or Field Guide expect OASIS-shaped routes (especially `/api/avatar/*` and JWT claim `id` = avatar GUID), implement thin AZOA adapters that speak that wire contract using AZOA-native identity types.
3. **Apache-only dependencies** in the fork. Do not introduce GPL islands or proprietary OASIS packages.
4. **Keep Jade syncable.** Prefer additive modules and adapters over renames that make `git merge upstream` painful. Periodically `git fetch upstream` and merge or rebase.
5. **No Karma / HyperDrive / HoloNET / MEF** as part of this cutover. Those stay out of AZOA by product design.
6. **Launchboard is parked.** See `Docs/Launchboard/AZOA_MIGRATION_ONE_PAGER_FOR_JADE.md` for a later adapter cutover; do not expand this fork work into CMTAT/Launchboard until Field Guide auth works.

## Allowed patterns

- Reimplement route handlers that match hitchhikers-auth request/response shapes.
- New AZOA MCP tools or HTTP routes that Field Guide can call.
- Client-side config flips (`oasisBaseUrl` → AZOA fork staging URL).
- Docs and gap matrix under `Docs/AZOA_GAP/`.

## Forbidden patterns

- `cp` / paste of OASIS `.cs` managers into AZOA.
- Vendoring NextGenSoftware OASIS NuGet packages into AZOA for “compatibility.”
- Renaming AZOA entities back to OASIS brand names in the public product surface (internal compat aliases are fine).

## First success criteria

- hitchhikers-auth login against the Max AZOA fork succeeds.
- Field Guide MCP can wrap that JWT (`fgat_*`).
- Trust Graph UI resolves the same avatar id.
- Gap matrix lists remaining Web4-only items as Later / Never with IP notes.
