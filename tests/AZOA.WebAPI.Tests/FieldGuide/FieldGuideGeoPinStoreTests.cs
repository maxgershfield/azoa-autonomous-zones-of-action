using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Services.FieldGuide;

namespace AZOA.WebAPI.Tests.FieldGuide;

public class FieldGuideGeoPinStoreTests
{
    [Fact]
    public void Nearby_FiltersByHaversineRadius()
    {
        var store = new FieldGuideGeoPinStore();
        store.Place(new FieldGuidePlacePinRequest
        {
            Name = "Near",
            Latitude = 51.52,
            Longitude = -0.1,
            TrailPinId = "near-1",
        }, Guid.NewGuid());
        store.Place(new FieldGuidePlacePinRequest
        {
            Name = "Far",
            Latitude = 40.0,
            Longitude = -74.0,
            TrailPinId = "far-1",
        }, Guid.NewGuid());

        var nearby = store.Nearby(51.52, -0.1, radiusKm: 2);
        Assert.Single(nearby);
        Assert.Equal("Near", nearby[0].Name);
        Assert.True(nearby[0].MetaData.ContainsKey("lat"));
        Assert.True(nearby[0].MetaData.ContainsKey("long"));
    }
}
