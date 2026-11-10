// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;

namespace AZOA.WebAPI.Models.FieldGuide;

public sealed class FieldGuideSaveHolonBody
{
    public FieldGuideHolonPayload? Holon { get; set; }
    public bool SaveChildren { get; set; }
    public bool Recursive { get; set; }
    public int MaxChildDepth { get; set; }
    public bool ContinueOnError { get; set; }
    public string? ProviderType { get; set; }
    public bool SetGlobally { get; set; }
}

public sealed class FieldGuideHolonPayload
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ParentHolonId { get; set; }
    public string? HolonType { get; set; }
    public Dictionary<string, JsonElement>? MetaData { get; set; }
}

public sealed class FieldGuideLoadHolonBody
{
    public string? Id { get; set; }
    public bool LoadChildren { get; set; }
    public bool Recursive { get; set; }
    public int MaxChildDepth { get; set; }
    public bool ContinueOnError { get; set; }
    public int Version { get; set; }
    public string? ProviderType { get; set; }
    public bool SetGlobally { get; set; }
}

public sealed class FieldGuideMintNftBody
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Symbol { get; set; }
    public int NumberToMint { get; set; } = 1;
    public string? OnChainProvider { get; set; }
    public string? OffChainProvider { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? MemoText { get; set; }
    public string? SendToAddressAfterMinting { get; set; }
    public string? SendToAvatarAfterMintingId { get; set; }
    public Dictionary<string, JsonElement>? MetaData { get; set; }
}

public sealed class FieldGuideKeysProvisionBody
{
    public string? AvatarID { get; set; }
    public string? AvatarEmail { get; set; }
    public string? ProviderType { get; set; }
}
