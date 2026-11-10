// SPDX-License-Identifier: Apache-2.0
// OASIS /api/nft/* facade. Collectibles are AZOA holons (AssetType=fg-collectible)
// parented under User.Id until FG JWT holders carry AZOA NftMint signing scope.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Interfaces.Managers;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Models.Requests;
using AZOA.WebAPI.Models.Responses;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/nft")]
[Authorize]
public sealed class NftCompatController : ControllerBase
{
    private readonly IHolonManager _holons;

    public NftCompatController(IHolonManager holons)
    {
        _holons = holons;
    }

    [HttpGet("load-all-nfts-for_avatar/{avatarId}")]
    public Task<ActionResult<AZOAResult<object>>> LoadAllForAvatar(string avatarId) =>
        LoadCollectibles(avatarId);

    [HttpGet("load-all-web3-nfts-for-avatar/{avatarId}")]
    public Task<ActionResult<AZOAResult<object>>> LoadAllWeb3ForAvatar(string avatarId) =>
        LoadCollectibles(avatarId);

    [HttpPost("mint-nft")]
    public async Task<ActionResult<AZOAResult<object>>> MintNft([FromBody] FieldGuideMintNftBody body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        var title = (body.Title ?? string.Empty).Trim();
        if (title.Length == 0)
            return BadRequest(AZOAResult<object>.Failure("Title is required."));

        var symbol = (body.Symbol ?? string.Empty).Trim();
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (body.MetaData is not null)
        {
            foreach (var kv in body.MetaData)
            {
                meta[kv.Key] = kv.Value.ValueKind == JsonValueKind.String
                    ? kv.Value.GetString() ?? string.Empty
                    : kv.Value.ToString();
            }
        }

        if (!string.IsNullOrWhiteSpace(body.MemoText) && !meta.ContainsKey("trailPinId"))
            meta["trailPinId"] = body.MemoText.Trim();

        var trailPinId = meta.GetValueOrDefault("trailPinId") ?? string.Empty;
        if (trailPinId.Length > 0)
        {
            var existing = await FindByTrailPinAsync(userId.Value, trailPinId);
            if (existing is not null)
                return Ok(AZOAResult<object>.Success(existing, "Collectible already in wallet. Will not remint."));
        }

        Guid parent = userId.Value;
        if (Guid.TryParse(body.SendToAvatarAfterMintingId, out var sendTo) && sendTo == userId.Value)
            parent = sendTo;

        var model = FieldGuideHolonMapper.ToCollectibleCreate(
            title,
            body.Description ?? string.Empty,
            symbol,
            meta,
            body.ImageUrl ?? body.ThumbnailUrl,
            parent);

        var created = await _holons.CreateAsync(model, userId.Value);
        if (created.IsError || created.Result is null)
            return BadRequest(AZOAResult<object>.Failure(created.Message));

        return Ok(AZOAResult<object>.Success(
            FieldGuideHolonMapper.ToOasisCollectible(created.Result),
            $"{title} recorded in Field Guide collectible ledger."));
    }

    [HttpPost("mint-and-place-geo-nft")]
    public async Task<ActionResult<AZOAResult<object>>> MintAndPlace(
        [FromBody] FieldGuideMintNftBody body,
        [FromServices] IFieldGuideGeoPinStore pins)
    {
        var minted = await MintNft(body);
        if (minted.Result is not OkObjectResult { Value: AZOAResult<object> result }
            || result.IsError
            || result.Result is null)
        {
            return minted;
        }

        double? lat = null;
        double? lon = null;
        if (body.MetaData is not null)
        {
            if (body.MetaData.TryGetValue("lat", out var latEl) && latEl.TryGetDouble(out var latV))
                lat = latV;
            if (body.MetaData.TryGetValue("long", out var lonEl) && lonEl.TryGetDouble(out var lonV))
                lon = lonV;
            else if (body.MetaData.TryGetValue("lon", out var lon2) && lon2.TryGetDouble(out var lonV2))
                lon = lonV2;
        }

        if (lat is not null && lon is not null)
        {
            var userId = GetUserIdFromClaims();
            var trailPinId = body.MemoText
                             ?? (body.MetaData is not null
                                 && body.MetaData.TryGetValue("trailPinId", out var pinEl)
                                 ? pinEl.GetString()
                                 : null)
                             ?? $"drop-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var pin = pins.Place(new FieldGuidePlacePinRequest
            {
                Name = body.Title ?? "Field Drop",
                Description = body.Description ?? string.Empty,
                Latitude = lat.Value,
                Longitude = lon.Value,
                TrailPinId = trailPinId ?? string.Empty,
                Web4NftId = string.Empty,
                Kind = "drop",
                ImageUrl = body.ImageUrl ?? string.Empty,
            }, userId);
            return Ok(AZOAResult<object>.Success(new
            {
                nft = result.Result,
                pinId = pin.Id,
            }, "Minted and placed."));
        }

        return Ok(result);
    }

    private async Task<ActionResult<AZOAResult<object>>> LoadCollectibles(string avatarId)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(avatarId, out var avatar) || avatar != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("Cannot load NFTs for another avatar."));

        var query = await _holons.QueryAsync(new HolonQueryRequest
        {
            UserId = userId,
            AssetType = FieldGuideHolonMapper.CollectibleAssetType,
            IsActive = true,
        }, userId);
        if (query.IsError || query.Result is null)
            return Ok(AZOAResult<object>.Success(Array.Empty<object>(), query.Message));

        var mapped = query.Result.Select(FieldGuideHolonMapper.ToOasisCollectible).ToList();
        return Ok(AZOAResult<object>.Success(mapped));
    }

    private async Task<object?> FindByTrailPinAsync(Guid userId, string trailPinId)
    {
        var query = await _holons.QueryAsync(new HolonQueryRequest
        {
            UserId = userId,
            AssetType = FieldGuideHolonMapper.CollectibleAssetType,
            IsActive = true,
        }, userId);
        if (query.IsError || query.Result is null)
            return null;

        foreach (var h in query.Result)
        {
            if (h.Metadata.TryGetValue("trailPinId", out var pin)
                && string.Equals(pin, trailPinId, StringComparison.OrdinalIgnoreCase))
            {
                return FieldGuideHolonMapper.ToOasisCollectible(h);
            }
        }

        return null;
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
