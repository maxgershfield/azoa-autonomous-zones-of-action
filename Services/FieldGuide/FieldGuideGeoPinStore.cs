// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using AZOA.WebAPI.Models.FieldGuide;

namespace AZOA.WebAPI.Services.FieldGuide;

public sealed class FieldGuideGeoPinStore : IFieldGuideGeoPinStore
{
    private readonly ConcurrentDictionary<Guid, FieldGuideGeoPin> _pins = new();
    private readonly ConcurrentBag<FieldGuideStreetEventRequest> _streetEvents = new();

    public FieldGuideGeoPin Place(FieldGuidePlacePinRequest request, Guid? createdByUserId)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0)
            throw new ArgumentException("Name required");

        var trailPinId = string.IsNullOrWhiteSpace(request.TrailPinId)
            ? $"drop-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request.TrailPinId.Trim();

        var pin = new FieldGuideGeoPin
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description ?? string.Empty,
            MetaData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["lat"] = request.Latitude,
                ["long"] = request.Longitude,
                ["lon"] = request.Longitude,
                ["radiusM"] = request.RadiusM,
                ["trailPinId"] = trailPinId,
                ["trail"] = request.Trail ?? "star-drop",
                ["questRole"] = request.QuestRole ?? "drop",
                ["wikiSlug"] = request.WikiSlug ?? string.Empty,
                ["web4NftId"] = request.Web4NftId ?? string.Empty,
                ["questId"] = request.QuestId ?? string.Empty,
                ["source"] = request.Source ?? "field-guide-web",
                ["imageUrl"] = request.ImageUrl ?? string.Empty,
                ["fileUrl"] = request.FileUrl ?? string.Empty,
                ["kind"] = request.Kind ?? "drop",
                ["narrationText"] = request.NarrationText ?? string.Empty,
                ["markerStyle"] = request.MarkerStyle ?? string.Empty,
                ["createdByAvatarName"] = request.CreatedByAvatarName ?? string.Empty,
                ["createdByAvatarId"] = request.CreatedByAvatarId
                    ?? (createdByUserId?.ToString() ?? string.Empty),
            },
            STARNETDNA = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = trailPinId,
                ["name"] = name,
            },
        };

        _pins[pin.Id] = pin;
        return pin;
    }

    public IReadOnlyList<FieldGuideGeoPin> Nearby(double latitude, double longitude, double radiusKm)
    {
        var radius = radiusKm > 0 ? radiusKm : 5;
        return _pins.Values
            .Where(p =>
            {
                if (!TryLatLon(p, out var lat, out var lon))
                    return false;
                return HaversineKm(latitude, longitude, lat, lon) <= radius;
            })
            .ToList();
    }

    public void RecordStreetEvent(FieldGuideStreetEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TrailPinId))
            return;
        _streetEvents.Add(request);
    }

    private static bool TryLatLon(FieldGuideGeoPin pin, out double lat, out double lon)
    {
        lat = 0;
        lon = 0;
        if (!TryNumber(pin.MetaData, "lat", out lat))
            return false;
        if (TryNumber(pin.MetaData, "long", out lon)
            || TryNumber(pin.MetaData, "lon", out lon)
            || TryNumber(pin.MetaData, "lng", out lon))
            return true;
        return false;
    }

    private static bool TryNumber(Dictionary<string, object?> meta, string key, out double value)
    {
        value = 0;
        if (!meta.TryGetValue(key, out var raw) || raw is null)
            return false;
        switch (raw)
        {
            case double d:
                value = d;
                return true;
            case float f:
                value = f;
                return true;
            case int i:
                value = i;
                return true;
            case long l:
                value = l;
                return true;
            case decimal m:
                value = (double)m;
                return true;
            case string s when double.TryParse(s, out var parsed):
                value = parsed;
                return true;
            default:
                return false;
        }
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        static double Rad(double deg) => deg * Math.PI / 180.0;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }
}
