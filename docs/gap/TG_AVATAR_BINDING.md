# Trust Graph ↔ AZOA avatar id binding

## Rule

```
Field Guide avatarId  ≡  hitchhikers-auth avatarId  ≡  JWT claim "id"  ≡  AZOA User.Id
```

There is no second identity space. Trust Graph UI code under
`field-guide/finsbury-guide/src/trustGraph/` is mostly client-local projections
(personas, City Key, star glyph). It joins to server data via the same avatar GUID
the session already holds.

## Cutover checklist

1. Login via `/api/avatar/*` on the AZOA fork (compat facade).
2. Confirm JWT `id` and session `avatarId` match.
3. Trust Graph screens that show "me" continue to use `session.avatarId`.
4. ONODE `/api/trustgraph/*` (bootstrap/state/card) remains **P1** Missing on AZOA;
   holon-backed TG listener writes can use `/api/holon` later (see MATRIX.md).

## Do not

- Invent a parallel `trustGraphUserId`.
- Copy ONODE `TrustGraphController` into the AZOA fork.
- Put raw avatar ids on peer pins (see `field-guide/AGENTS.md`).
