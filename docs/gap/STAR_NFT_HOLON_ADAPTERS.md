# STAR / NFT / holon adapters (Field Guide cutover)

Clean-room facades on branch `field-guide-azoa-cutover`. No OASIS `.cs` paste.
Identity invariant: JWT `id` = `User.Id` = Field Guide `avatarId`.

## STAR-compat

| Method | Path | Backing |
|--------|------|---------|
| GET | `/api/Quests/all-for-avatar/game` | In-memory shared catalog + per-user progress |
| GET | `/api/Quests/{id}` | Same |
| POST | `/api/Quests/create` | Publishes to shared catalog |
| POST | `/api/Quests/{id}/start` | Marks started |
| POST | `/api/Quests/{id}/progress` | `ItemCollectedName` matches objective `Title` |
| POST | `/api/Quests/{id}/complete` | Completes all objectives |
| GET | `/api/GeoNFTs/nearby` | In-memory pins, Haversine filter (anonymous OK) |
| POST | `/api/GeoNFTs/place-pin` | Stores pin with `metaData.lat` / `long` |
| POST | `/api/GeoNFTs/street-event` | Ack + append event log |

Process-local until Surreal persistence is added. Fine for staging smoke; multi-instance Railway needs a store follow-up.

## Holon `/api/data`

| Method | Path | Backing |
|--------|------|---------|
| POST | `/api/data/save-holon` | `IHolonManager` create/update, `AssetType=field-guide` |
| POST | `/api/data/load-holon` | `GetAsync` scoped to caller |
| GET | `/api/data/load-holons-for-parent/{avatarId}/...` | `GetChildrenAsync(avatarId)`; avatarId must equal caller |
| GET | `/api/data/load-holons-by-metadata` | Query `field-guide` holons; filter flat Metadata |

Nested OASIS `metaData` is JSON-serialized into `Metadata["__fgMetaJson"]`; string fields such as `kind` are also flattened for query. Parent is `User.Id` (FG avatar parent convention).

## NFT / wallet / keys

| Method | Path | Backing |
|--------|------|---------|
| GET | `/api/nft/load-all-nfts-for_avatar/{id}` | Holons `AssetType=fg-collectible` |
| GET | `/api/nft/load-all-web3-nfts-for-avatar/{id}` | Same ledger (OASIS-shaped) |
| POST | `/api/nft/mint-nft` | Create collectible holon; remint by `MetaData.trailPinId` |
| POST | `/api/nft/mint-and-place-geo-nft` | Mint + optional geo pin if lat/lon present |
| GET | `/api/wallet/avatar/{id}/wallets/{loadBalance}/{loadNfts}` | `IWalletManager.QueryAsync` keyed as `SolanaOASIS` |
| GET | `/api/wallet/avatar/{id}/default-wallet` | Default / first matching chain |
| GET | `/api/wallet/avatar/{id}/wallet/{walletId}/tokens` | Native balance only (honest) |
| POST | `/api/keys/generate_keypair_..._by_id` | `BootstrapWalletAsync(Solana)` |
| POST | `/api/keys/generate_keypair_..._by_email` | Same after email match |

Collectible mint is a **ledger holon**, not AZOA on-chain `NftManager.MintAsync`. FG avatar JWTs do not carry `NftMint` signing scope yet. Remint checks and wallet NFT lists work. On-chain SPL mint is a follow-up once custody + scopes are granted to FirstPartyLogin users.

Keys provision requires node custody (`WALLET_CUSTODY_UNAVAILABLE` if the node refuses to hold keys).

## Client pointing

Point both STAR and OASIS base URLs at the same AZOA staging host (see [CLIENT_POINTING.md](CLIENT_POINTING.md)).
