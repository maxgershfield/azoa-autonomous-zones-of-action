// SPDX-License-Identifier: Apache-2.0
// OASIS /api/data/* facade over AZOA HolonManager. Parent = avatar User.Id.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Interfaces.Managers;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Models.Requests;
using AZOA.WebAPI.Models.Responses;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/data")]
[Authorize]
public sealed class DataHolonCompatController : ControllerBase
{
    private readonly IHolonManager _holons;

    public DataHolonCompatController(IHolonManager holons)
    {
        _holons = holons;
    }

    [HttpPost("save-holon")]
    public async Task<ActionResult<AZOAResult<object>>> SaveHolon([FromBody] FieldGuideSaveHolonBody body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        try
        {
            if (body.Holon is not null
                && Guid.TryParse(body.Holon.Id, out var existingId)
                && existingId != Guid.Empty)
            {
                var updated = await _holons.UpdateAsync(existingId, FieldGuideHolonMapper.ToUpdateModel(body), userId);
                if (updated.IsError || updated.Result is null)
                    return BadRequest(AZOAResult<object>.Failure(updated.Message));
                return Ok(AZOAResult<object>.Success(FieldGuideHolonMapper.ToOasisHolon(updated.Result), "Holon saved."));
            }

            var created = await _holons.CreateAsync(FieldGuideHolonMapper.ToCreateModel(body, userId.Value), userId.Value);
            if (created.IsError || created.Result is null)
                return BadRequest(AZOAResult<object>.Failure(created.Message));
            return Ok(AZOAResult<object>.Success(FieldGuideHolonMapper.ToOasisHolon(created.Result), "Holon saved."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(AZOAResult<object>.Failure(ex.Message));
        }
    }

    [HttpPost("load-holon")]
    public async Task<ActionResult<AZOAResult<object>>> LoadHolon([FromBody] FieldGuideLoadHolonBody body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(body.Id, out var id))
            return BadRequest(AZOAResult<object>.Failure("id is required."));

        var loaded = await _holons.GetAsync(id, userId);
        if (loaded.IsError || loaded.Result is null)
            return NotFound(AZOAResult<object>.Failure(loaded.Message));
        return Ok(AZOAResult<object>.Success(FieldGuideHolonMapper.ToOasisHolon(loaded.Result)));
    }

    [HttpGet("load-holons-for-parent/{parentId}/{holonType}/{loadChildren}/{recursive}/{maxChildDepth}/{continueOnError}/{version}")]
    public async Task<ActionResult<AZOAResult<object>>> LoadHolonsForParent(
        string parentId,
        string holonType,
        bool loadChildren,
        bool recursive,
        int maxChildDepth,
        bool continueOnError,
        int version)
    {
        _ = (holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(parentId, out var parent))
            return BadRequest(AZOAResult<object>.Failure("parentId must be a GUID."));

        // FG parents notes/events under avatarId (= User.Id). Scope to caller.
        if (parent != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("Cannot load holons for another avatar."));

        var children = await _holons.GetChildrenAsync(parent, userId);
        if (children.IsError || children.Result is null)
            return Ok(AZOAResult<object>.Success(Array.Empty<object>(), children.Message));

        var mapped = children.Result
            .Where(h => string.Equals(h.AssetType, FieldGuideHolonMapper.AssetType, StringComparison.Ordinal)
                        || string.Equals(h.AssetType, FieldGuideHolonMapper.CollectibleAssetType, StringComparison.Ordinal)
                        || string.IsNullOrEmpty(h.AssetType))
            .Select(FieldGuideHolonMapper.ToOasisHolon)
            .ToList();
        return Ok(AZOAResult<object>.Success(mapped));
    }

    [HttpGet("load-holons-by-metadata")]
    public async Task<ActionResult<AZOAResult<object>>> LoadHolonsByMetadata(
        [FromQuery] string metaKey,
        [FromQuery] string? metaValue = null)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (string.IsNullOrWhiteSpace(metaKey))
            return BadRequest(AZOAResult<object>.Failure("metaKey is required."));

        var all = await _holons.QueryAsync(new HolonQueryRequest
        {
            UserId = userId,
            AssetType = FieldGuideHolonMapper.AssetType,
            IsActive = true,
        }, userId);
        if (all.IsError || all.Result is null)
            return Ok(AZOAResult<object>.Success(Array.Empty<object>(), all.Message));

        var key = metaKey.Trim();
        var want = metaValue?.Trim();
        var mapped = all.Result
            .Where(h =>
            {
                if (!h.Metadata.TryGetValue(key, out var value))
                    return false;
                return string.IsNullOrEmpty(want)
                       || string.Equals(value, want, StringComparison.OrdinalIgnoreCase);
            })
            .Select(FieldGuideHolonMapper.ToOasisHolon)
            .ToList();
        return Ok(AZOAResult<object>.Success(mapped));
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
