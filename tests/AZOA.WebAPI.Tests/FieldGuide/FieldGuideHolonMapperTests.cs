using System.Text.Json;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Tests.FieldGuide;

public class FieldGuideHolonMapperTests
{
    [Fact]
    public void ToCreateModel_ParentsUnderAvatarId_AndPreservesKind()
    {
        var avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var metaJson = JsonSerializer.SerializeToElement(new Dictionary<string, object>
        {
            ["kind"] = "personal-note",
            ["payload"] = new { text = "hello" },
        });
        var meta = metaJson.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value, StringComparer.OrdinalIgnoreCase);

        var model = FieldGuideHolonMapper.ToCreateModel(new FieldGuideSaveHolonBody
        {
            Holon = new FieldGuideHolonPayload
            {
                Name = "Note",
                ParentHolonId = avatarId.ToString(),
                MetaData = meta,
            },
        }, avatarId);

        Assert.Equal(avatarId, model.ParentHolonId);
        Assert.Equal(FieldGuideHolonMapper.AssetType, model.AssetType);
        Assert.Equal("personal-note", model.Metadata["kind"]);
        Assert.True(model.Metadata.ContainsKey(FieldGuideHolonMapper.MetaJsonKey));
    }
}
