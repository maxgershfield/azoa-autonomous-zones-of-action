# CHAINS.md — Web4 vs AZOA blockchain rails

Clean-room only. Do not paste OASIS `*OASIS.cs` / Web3Core into the fork.

## AZOA shipped today

| Chain | Role | Notes |
|-------|------|--------|
| **Algorand** | Primary rail | Wallets, NFT, DEX, exchange, bridge source, faucet |
| **Solana** | Second rail | NFT/SPL/DEX; FG cutover default; no bridge legs yet |
| **Ethereum** | Third rail | Provider registered; networks often `IsEnabled: false` until RPC + conformance |
| **Simulated** | Dev/test | Boot-guarded out of production |

Factory honesty list (`Program.cs`): `Algorand`, `Solana`, `Ethereum`, `Simulated`.

Wallet bootstrap canonicalizes only Algorand / Solana / Ethereum.

## Web4 catalog (not a port list)

`ProviderType` enumerates ~40 chain names. BootLoader wires a smaller active set
(Solana, Ethereum, Arbitrum, Polygon, Rootstock, Monad, Avalanche, Base, plus
several non-EVM). Many L2s are thin EVM wrappers. Algorand is **absent** from Web4.

## Gap → port style

| Target | Approach |
|--------|----------|
| Enable Ethereum Devnet/Testnet | Config + secrets + conformance (not new code) |
| Polygon, Arbitrum, Base, Avalanche, Monad, Rootstock, Optimism, … | **Ethereum network profiles** (chainId / RPC / CAIP-2) under existing Ethereum family; clean-room |
| EOS / Telos / SEEDS / TRON / Cosmos / Bitcoin / NEAR / Sui / Aptos / … | Full new `Providers/Blockchain/<Family>/` + signer + SDK + conformance |
| FG wallet/NFT today | Keep Solana via compat facades; add Ethereum (+ L2 aliases) when product needs them |
| HyperDrive / Karma / Holochain / MEF | Never |

## Blockers for every new rail

- Apache-only deps; no NextGen NuGet
- Custody + chain-stamped signing context + scopes
- Factory registration must match `Blockchain:Chains` (boot fails otherwise)
- Conformance battery before advertising capability
- Bridge/DEX legs are separate modules, not a boolean

## Recommended next slice (when prioritized)

1. Enable Ethereum on staging with RPC secrets.
2. Add Polygon + Base (+ Arbitrum) as Ethereum network profiles.
3. Widen Field Guide wallet/keys compat beyond Solana-only.
4. Defer non-EVM Web4 chains unless a product ticket names them.
