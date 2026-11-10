// SPDX-License-Identifier: Apache-2.0
// Shared STAR-compat quest catalog + per-user progress for Field Guide.

using AZOA.WebAPI.Models.FieldGuide;

namespace AZOA.WebAPI.Services.FieldGuide;

public interface IFieldGuideQuestCatalog
{
    IReadOnlyList<FieldGuideQuestDefinition> ListDefinitions();
    FieldGuideQuestDefinition? GetDefinition(Guid questId);
    FieldGuideQuestDefinition Create(FieldGuideQuestCreateRequest request, Guid authorUserId);
    FieldGuideQuestProgress GetOrCreateProgress(Guid userId, Guid questId);
    FieldGuideQuestProgress Start(Guid userId, Guid questId);
    FieldGuideQuestProgress Progress(Guid userId, Guid questId, string itemCollectedName);
    FieldGuideQuestProgress Complete(Guid userId, Guid questId);
    FieldGuideQuestView ToView(FieldGuideQuestDefinition def, FieldGuideQuestProgress? progress);
}
