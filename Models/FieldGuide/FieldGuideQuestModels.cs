// SPDX-License-Identifier: Apache-2.0
// Wire shapes for STAR /api/Quests used by field-guide-web4-client.

namespace AZOA.WebAPI.Models.FieldGuide;

public sealed class FieldGuideQuestObjectiveDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public string GameSource { get; set; } = "FieldGuide";
}

public sealed class FieldGuideQuestDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GameSource { get; set; } = "FieldGuide";
    public string ExternalHandoffUri { get; set; } = string.Empty;
    public Guid? AuthorUserId { get; set; }
    public List<FieldGuideQuestObjectiveDefinition> Objectives { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FieldGuideQuestProgress
{
    public Guid UserId { get; set; }
    public Guid QuestId { get; set; }
    public bool Started { get; set; }
    public bool Completed { get; set; }
    public HashSet<string> CompletedPinTitles { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FieldGuideQuestObjectiveView
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public string ProgressSummary { get; set; } = string.Empty;
    public double ProgressPercent { get; set; }
}

public sealed class FieldGuideQuestView
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public double ProgressPercent { get; set; }
    public string GameSource { get; set; } = "FieldGuide";
    public string ExternalHandoffUri { get; set; } = string.Empty;
    public string TrailFile { get; set; } = string.Empty;
    public string Atmosphere { get; set; } = string.Empty;
    public List<FieldGuideQuestObjectiveView> Objectives { get; set; } = new();
}

public sealed class FieldGuideQuestCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? HolonSubType { get; set; }
    public string? SourceFolderPath { get; set; }
    public string? GameSource { get; set; }
    public string? ExternalHandoffUri { get; set; }
    public List<FieldGuideQuestCreateObjective>? Objectives { get; set; }
}

public sealed class FieldGuideQuestCreateObjective
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? GameSource { get; set; }
    public int Order { get; set; }
}

public sealed class FieldGuideQuestProgressRequest
{
    public string? GameSource { get; set; }
    public int GenericItemPickup { get; set; }
    public string? ItemCollectedName { get; set; }
}
