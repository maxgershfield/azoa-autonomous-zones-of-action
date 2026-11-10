# IP.md — AZOA Field Guide cutover

## Default: clean-room reimplement

Port wire contracts and behavior only. No OASIS `.cs` copies into the fork.
AZOA LICENSE: Apache 2.0, Copyright 2026 Jade Zaher. Keep notices; stay Apache-only deps.

## Safe

- Route/JSON/JWT contract facades on AZOA-native managers
- Client `oasisBaseUrl` flips; FG MCP `fgat_*` wrapping
- Additive modules; Jade upstream mergeable
- Interim: call Apache-pinned ONODE over HTTP (do not vendor into AZOA)

## High risk (reject)

- Paste from OASIS Architecture / ONODE / STAR ODK / Providers
- NextGen NuGet or GPL Holochain into AZOA
- Karma / HyperDrive / HoloNET / MEF
- Public rebrand of AZOA as OASIS

## Checklist

- [ ] No OASIS source paste
- [ ] No NextGen/GPL packages
- [ ] JWT `id` facade written from scratch
- [ ] Launchboard parked until FG auth works
