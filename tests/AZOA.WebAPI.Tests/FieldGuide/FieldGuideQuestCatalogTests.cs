using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Tests.FieldGuide;

public class FieldGuideQuestCatalogTests
{
    [Fact]
    public void Progress_MatchesObjectiveTitle_CaseInsensitive()
    {
        var catalog = new FieldGuideQuestCatalog();
        var userId = Guid.NewGuid();
        var def = catalog.Create(new FieldGuideQuestCreateRequest
        {
            Name = "JAB Gloves",
            Description = "[trailFile=jab] [atmosphere=street]",
            Objectives =
            [
                new FieldGuideQuestCreateObjective { Title = "pin-alpha", Order = 0 },
                new FieldGuideQuestCreateObjective { Title = "pin-beta", Order = 1 },
            ],
        }, userId);

        catalog.Start(userId, def.Id);
        catalog.Progress(userId, def.Id, "PIN-ALPHA");

        var view = catalog.ToView(def, catalog.GetOrCreateProgress(userId, def.Id));
        Assert.Equal("InProgress", view.Status);
        Assert.True(view.Objectives[0].IsCompleted);
        Assert.False(view.Objectives[1].IsCompleted);
        Assert.Equal("jab", view.TrailFile);
        Assert.Equal("street", view.Atmosphere);
    }

    [Fact]
    public void Progress_CompletesWhenAllPinsCollected()
    {
        var catalog = new FieldGuideQuestCatalog();
        var userId = Guid.NewGuid();
        var def = catalog.Create(new FieldGuideQuestCreateRequest
        {
            Name = "Sit",
            Objectives =
            [
                new FieldGuideQuestCreateObjective { Title = "a", Order = 0 },
                new FieldGuideQuestCreateObjective { Title = "b", Order = 1 },
            ],
        }, userId);

        catalog.Progress(userId, def.Id, "a");
        catalog.Progress(userId, def.Id, "b");

        var view = catalog.ToView(def, catalog.GetOrCreateProgress(userId, def.Id));
        Assert.Equal("Completed", view.Status);
        Assert.Equal(100, view.ProgressPercent);
    }
}
