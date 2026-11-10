// SPDX-License-Identifier: Apache-2.0
// STAR /api/GeoNFTs facade for Field Guide. Clean-room; no OASIS source.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Models.Responses;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/GeoNFTs")]
public sealed class StarGeoNftCompatController : ControllerBase
{
    private readonly IFieldGuideGeoPinStore _pins;

    public StarGeoNftCompatController(IFieldGuideGeoPinStore pins)
    {
        _pins = pins;
    }

    [HttpGet("nearby")]
    [AllowAnonymous]
    public ActionResult<AZOAResult<IReadOnlyList<FieldGuideGeoPin>>> Nearby(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 5)
    {
        var pins = _pins.Nearby(latitude, longitude, radiusKm);
        return Ok(AZOAResult<IReadOnlyList<FieldGuideGeoPin>>.Success(pins));
    }

    [HttpPost("place-pin")]
    [Authorize]
    public ActionResult<AZOAResult<FieldGuideGeoPin>> PlacePin([FromBody] FieldGuidePlacePinRequest body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<FieldGuideGeoPin>.Failure("Invalid token."));

        try
        {
            var pin = _pins.Place(body, userId);
            return Ok(AZOAResult<FieldGuideGeoPin>.Success(pin, "Pin placed."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(AZOAResult<FieldGuideGeoPin>.Failure(ex.Message));
        }
    }

    [HttpPost("street-event")]
    [Authorize]
    public ActionResult<AZOAResult<object>> StreetEvent([FromBody] FieldGuideStreetEventRequest body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        if (string.IsNullOrWhiteSpace(body.AvatarId))
            body.AvatarId = userId.Value.ToString();

        _pins.RecordStreetEvent(body);
        return Ok(AZOAResult<object>.Success(new { ok = true }, "Street event recorded."));
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
