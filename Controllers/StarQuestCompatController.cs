// SPDX-License-Identifier: Apache-2.0
// STAR /api/Quests facade for Field Guide. Clean-room; no OASIS source.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Models.Responses;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/Quests")]
[Authorize]
public sealed class StarQuestCompatController : ControllerBase
{
    private readonly IFieldGuideQuestCatalog _catalog;

    public StarQuestCompatController(IFieldGuideQuestCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("all-for-avatar/game")]
    public ActionResult<AZOAResult<IReadOnlyList<FieldGuideQuestView>>> AllForAvatarGame()
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<IReadOnlyList<FieldGuideQuestView>>.Failure("Invalid token."));

        var list = _catalog.ListDefinitions()
            .Select(def => _catalog.ToView(def, _catalog.GetOrCreateProgress(userId.Value, def.Id)))
            .ToList();
        return Ok(AZOAResult<IReadOnlyList<FieldGuideQuestView>>.Success(list));
    }

    [HttpGet("{questId:guid}")]
    public ActionResult<AZOAResult<FieldGuideQuestView>> Get(Guid questId)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<FieldGuideQuestView>.Failure("Invalid token."));

        var def = _catalog.GetDefinition(questId);
        if (def is null)
            return NotFound(AZOAResult<FieldGuideQuestView>.Failure("Quest not found."));

        var view = _catalog.ToView(def, _catalog.GetOrCreateProgress(userId.Value, questId));
        return Ok(AZOAResult<FieldGuideQuestView>.Success(view));
    }

    [HttpPost("create")]
    public ActionResult<AZOAResult<FieldGuideQuestView>> Create([FromBody] FieldGuideQuestCreateRequest body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<FieldGuideQuestView>.Failure("Invalid token."));

        try
        {
            var def = _catalog.Create(body, userId.Value);
            var view = _catalog.ToView(def, null);
            return Ok(AZOAResult<FieldGuideQuestView>.Success(view, "Quest created."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(AZOAResult<FieldGuideQuestView>.Failure(ex.Message));
        }
    }

    [HttpPost("{questId:guid}/start")]
    public ActionResult<AZOAResult<object>> Start(Guid questId)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        try
        {
            _catalog.Start(userId.Value, questId);
            return Ok(AZOAResult<object>.Success(new { id = questId }, "Quest started."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(AZOAResult<object>.Failure("Quest not found."));
        }
    }

    [HttpPost("{questId:guid}/progress")]
    public ActionResult<AZOAResult<object>> Progress(Guid questId, [FromBody] FieldGuideQuestProgressRequest body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        try
        {
            var progress = _catalog.Progress(userId.Value, questId, body.ItemCollectedName ?? string.Empty);
            return Ok(AZOAResult<object>.Success(new
            {
                id = questId,
                completed = progress.Completed,
                completedPins = progress.CompletedPinTitles.ToList(),
            }, "Progress recorded."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(AZOAResult<object>.Failure("Quest not found."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(AZOAResult<object>.Failure(ex.Message));
        }
    }

    [HttpPost("{questId:guid}/complete")]
    public ActionResult<AZOAResult<object>> Complete(Guid questId)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        try
        {
            _catalog.Complete(userId.Value, questId);
            return Ok(AZOAResult<object>.Success(new { id = questId }, "Quest completed."));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(AZOAResult<object>.Failure("Quest not found."));
        }
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
