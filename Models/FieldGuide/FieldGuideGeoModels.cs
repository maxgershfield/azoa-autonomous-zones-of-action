// SPDX-License-Identifier: Apache-2.0
// Wire shapes for STAR /api/GeoNFTs used by field-guide-web4-client.

namespace AZOA.WebAPI.Models.FieldGuide;

public sealed class FieldGuideGeoPin
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object?> MetaData { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, object?> STARNETDNA { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FieldGuidePlacePinRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusM { get; set; } = 75;
    public string TrailPinId { get; set; } = string.Empty;
    public string Trail { get; set; } = "star-drop";
    public string QuestRole { get; set; } = "drop";
    public string WikiSlug { get; set; } = string.Empty;
    public string Web4NftId { get; set; } = string.Empty;
    public string QuestId { get; set; } = string.Empty;
    public string Source { get; set; } = "field-guide-web";
    public string ImageUrl { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string Kind { get; set; } = "drop";
    public string NarrationText { get; set; } = string.Empty;
    public string MarkerStyle { get; set; } = string.Empty;
    public string CreatedByAvatarName { get; set; } = string.Empty;
    public string CreatedByAvatarId { get; set; } = string.Empty;
}

public sealed class FieldGuideStreetEventRequest
{
    public string TrailPinId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string AvatarName { get; set; } = string.Empty;
    public string AvatarId { get; set; } = string.Empty;
}
