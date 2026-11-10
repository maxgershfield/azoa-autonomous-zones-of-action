// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AZOA.WebAPI.Models.FieldGuide;

namespace AZOA.WebAPI.Services.FieldGuide;

public sealed class FieldGuideQuestCatalog : IFieldGuideQuestCatalog
{
    private readonly ConcurrentDictionary<Guid, FieldGuideQuestDefinition> _defs = new();
    private readonly ConcurrentDictionary<string, FieldGuideQuestProgress> _progress = new();

    public IReadOnlyList<FieldGuideQuestDefinition> ListDefinitions() =>
        _defs.Values.OrderByDescending(d => d.CreatedAt).ToList();

    public FieldGuideQuestDefinition? GetDefinition(Guid questId) =>
        _defs.TryGetValue(questId, out var def) ? def : null;

    public FieldGuideQuestDefinition Create(FieldGuideQuestCreateRequest request, Guid authorUserId)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Quest name is required.");

        var objectives = (request.Objectives ?? new List<FieldGuideQuestCreateObjective>())
            .Select((o, i) => new FieldGuideQuestObjectiveDefinition
            {
                Id = Guid.NewGuid(),
                Title = (o.Title ?? string.Empty).Trim(),
                Description = o.Description ?? string.Empty,
                Order = o.Order != 0 ? o.Order : i,
                GameSource = string.IsNullOrWhiteSpace(o.GameSource) ? "FieldGuide" : o.GameSource.Trim(),
            })
            .Where(o => o.Title.Length > 0)
            .OrderBy(o => o.Order)
            .ToList();

        var def = new FieldGuideQuestDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description ?? string.Empty,
            GameSource = string.IsNullOrWhiteSpace(request.GameSource) ? "FieldGuide" : request.GameSource.Trim(),
            ExternalHandoffUri = request.ExternalHandoffUri ?? string.Empty,
            AuthorUserId = authorUserId,
            Objectives = objectives,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _defs[def.Id] = def;
        return def;
    }

    public FieldGuideQuestProgress GetOrCreateProgress(Guid userId, Guid questId)
    {
        var key = ProgressKey(userId, questId);
        return _progress.GetOrAdd(key, _ => new FieldGuideQuestProgress
        {
            UserId = userId,
            QuestId = questId,
        });
    }

    public FieldGuideQuestProgress Start(Guid userId, Guid questId)
    {
        if (!_defs.ContainsKey(questId))
            throw new KeyNotFoundException("Quest not found.");
        var progress = GetOrCreateProgress(userId, questId);
        progress.Started = true;
        return progress;
    }

    public FieldGuideQuestProgress Progress(Guid userId, Guid questId, string itemCollectedName)
    {
        if (!_defs.TryGetValue(questId, out var def))
            throw new KeyNotFoundException("Quest not found.");

        var pin = (itemCollectedName ?? string.Empty).Trim();
        if (pin.Length == 0)
            throw new ArgumentException("ItemCollectedName is required.");

        var progress = GetOrCreateProgress(userId, questId);
        progress.Started = true;

        var match = def.Objectives.FirstOrDefault(o =>
            string.Equals(o.Title, pin, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            throw new ArgumentException($"No objective titled '{pin}' on this quest.");

        progress.CompletedPinTitles.Add(match.Title);
        if (def.Objectives.Count > 0
            && def.Objectives.All(o => progress.CompletedPinTitles.Contains(o.Title)))
        {
            progress.Completed = true;
        }

        return progress;
    }

    public FieldGuideQuestProgress Complete(Guid userId, Guid questId)
    {
        if (!_defs.TryGetValue(questId, out var def))
            throw new KeyNotFoundException("Quest not found.");

        var progress = GetOrCreateProgress(userId, questId);
        progress.Started = true;
        foreach (var o in def.Objectives)
            progress.CompletedPinTitles.Add(o.Title);
        progress.Completed = true;
        return progress;
    }

    public FieldGuideQuestView ToView(FieldGuideQuestDefinition def, FieldGuideQuestProgress? progress)
    {
        var completed = progress?.CompletedPinTitles ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var objectives = def.Objectives
            .OrderBy(o => o.Order)
            .Select(o =>
            {
                var done = completed.Contains(o.Title);
                return new FieldGuideQuestObjectiveView
                {
                    Id = o.Id,
                    Title = o.Title,
                    Description = o.Description,
                    Order = o.Order,
                    IsCompleted = done,
                    ProgressSummary = done ? "Completed" : "Not started",
                    ProgressPercent = done ? 100 : 0,
                };
            })
            .ToList();

        var total = objectives.Count;
        var doneCount = objectives.Count(o => o.IsCompleted);
        var percent = total == 0 ? 0 : Math.Round(100.0 * doneCount / total, 1);
        var status = progress?.Completed == true
            ? "Completed"
            : progress?.Started == true || doneCount > 0
                ? "InProgress"
                : "NotStarted";

        var (trail, atmosphere) = ParseMetaFromDescription(def.Description);
        return new FieldGuideQuestView
        {
            Id = def.Id,
            Name = def.Name,
            Description = def.Description,
            Status = status,
            ProgressPercent = percent,
            GameSource = def.GameSource,
            ExternalHandoffUri = def.ExternalHandoffUri,
            TrailFile = trail,
            Atmosphere = atmosphere,
            Objectives = objectives,
        };
    }

    private static string ProgressKey(Guid userId, Guid questId) =>
        $"{userId:N}:{questId:N}";

    private static (string TrailFile, string Atmosphere) ParseMetaFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return (string.Empty, string.Empty);
        var trail = Regex.Match(description, @"\[trailFile=([^\]]+)\]", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (trail.Length == 0)
            trail = Regex.Match(description, @"trailFile\s*=\s*([^\s,\]]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        var atmos = Regex.Match(description, @"\[atmosphere=([^\]]+)\]", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (atmos.Length == 0)
            atmos = Regex.Match(description, @"atmosphere\s*=\s*([^\s,\]]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        return (trail, atmos);
    }
}
