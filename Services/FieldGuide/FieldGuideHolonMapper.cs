// SPDX-License-Identifier: Apache-2.0
// Maps OASIS /api/data holon JSON (nested metaData) onto AZOA Holon Metadata strings.

using System.Text.Json;
using AZOA.WebAPI.Interfaces;
using AZOA.WebAPI.Models;
using AZOA.WebAPI.Models.FieldGuide;

namespace AZOA.WebAPI.Services.FieldGuide;

public static class FieldGuideHolonMapper
{
    public const string AssetType = "field-guide";
    public const string CollectibleAssetType = "fg-collectible";
    public const string MetaJsonKey = "__fgMetaJson";
    public const string KindKey = "kind";

    public static HolonCreateModel ToCreateModel(FieldGuideSaveHolonBody body, Guid callerUserId)
    {
        var holon = body.Holon ?? throw new ArgumentException("holon is required.");
        var meta = FlattenMeta(holon.MetaData);
        Guid? parentId = null;
        if (Guid.TryParse(holon.ParentHolonId, out var parsedParent))
            parentId = parsedParent;
        else
            parentId = callerUserId;

        return new HolonCreateModel
        {
            Name = string.IsNullOrWhiteSpace(holon.Name) ? "Field Guide Holon" : holon.Name.Trim(),
            Description = holon.Description ?? string.Empty,
            ParentHolonId = parentId,
            ProviderName = "FieldGuideCompat",
            AssetType = AssetType,
            Metadata = meta,
            IsPublic = false,
        };
    }

    public static HolonUpdateModel ToUpdateModel(FieldGuideSaveHolonBody body)
    {
        var holon = body.Holon ?? throw new ArgumentException("holon is required.");
        Guid? parentId = null;
        if (Guid.TryParse(holon.ParentHolonId, out var parsedParent))
            parentId = parsedParent;

        return new HolonUpdateModel
        {
            Name = holon.Name,
            Description = holon.Description,
            ParentHolonId = parentId,
            AssetType = AssetType,
            Metadata = FlattenMeta(holon.MetaData),
            IsActive = true,
        };
    }

    public static object ToOasisHolon(IHolon holon)
    {
        object meta;
        if (holon.Metadata.TryGetValue(MetaJsonKey, out var json) && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                meta = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                       ?? new Dictionary<string, JsonElement>();
            }
            catch (JsonException)
            {
                meta = holon.Metadata
                    .Where(kv => kv.Key != MetaJsonKey)
                    .ToDictionary(kv => kv.Key, kv => (object)kv.Value, StringComparer.OrdinalIgnoreCase);
            }
        }
        else
        {
            meta = holon.Metadata
                .Where(kv => kv.Key != MetaJsonKey)
                .ToDictionary(kv => kv.Key, kv => (object)kv.Value, StringComparer.OrdinalIgnoreCase);
        }

        return new
        {
            id = holon.Id,
            Id = holon.Id,
            name = holon.Name,
            Name = holon.Name,
            description = holon.Description,
            Description = holon.Description,
            parentHolonId = holon.ParentHolonId,
            ParentHolonId = holon.ParentHolonId,
            holonType = "Holon",
            HolonType = "Holon",
            metaData = meta,
            MetaData = meta,
            createdDate = holon.CreatedDate,
            CreatedDate = holon.CreatedDate,
            isActive = holon.IsActive,
            IsActive = holon.IsActive,
        };
    }

    public static object ToOasisCollectible(IHolon holon)
    {
        var meta = holon.Metadata
            .Where(kv => kv.Key != MetaJsonKey)
            .ToDictionary(kv => kv.Key, kv => (object)kv.Value, StringComparer.OrdinalIgnoreCase);
        if (holon.Metadata.TryGetValue(MetaJsonKey, out var json) && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var nested = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (nested is not null)
                {
                    foreach (var kv in nested)
                        meta[kv.Key] = kv.Value.ValueKind == JsonValueKind.String
                            ? kv.Value.GetString() ?? string.Empty
                            : kv.Value.ToString();
                }
            }
            catch (JsonException)
            {
                // keep flat metadata
            }
        }

        var tokenAddress = holon.Metadata.GetValueOrDefault("nftTokenAddress")
                           ?? holon.TokenId
                           ?? holon.Id.ToString("N");
        var mintHash = holon.Metadata.GetValueOrDefault("mintTransactionHash")
                       ?? $"fg-ledger:{holon.Id:N}";
        var symbol = holon.Metadata.GetValueOrDefault("symbol") ?? string.Empty;

        return new
        {
            id = holon.Id,
            Id = holon.Id,
            title = holon.Name,
            Title = holon.Name,
            name = holon.Name,
            Name = holon.Name,
            description = holon.Description,
            Description = holon.Description,
            symbol,
            Symbol = symbol,
            nftTokenAddress = tokenAddress,
            NFTTokenAddress = tokenAddress,
            mintTransactionHash = mintHash,
            MintTransactionHash = mintHash,
            metaData = meta,
            MetaData = meta,
            web3NFTs = new[]
            {
                new
                {
                    nftTokenAddress = tokenAddress,
                    NFTTokenAddress = tokenAddress,
                    mintTransactionHash = mintHash,
                    MintTransactionHash = mintHash,
                    symbol,
                    Symbol = symbol,
                },
            },
        };
    }

    public static HolonCreateModel ToCollectibleCreate(
        string title,
        string description,
        string symbol,
        Dictionary<string, string> meta,
        string? imageUrl,
        Guid parentAvatarId)
    {
        var payload = new Dictionary<string, string>(meta, StringComparer.OrdinalIgnoreCase)
        {
            ["symbol"] = symbol,
        };
        if (!string.IsNullOrWhiteSpace(imageUrl))
            payload["imageUrl"] = imageUrl;

        var idHint = Guid.NewGuid();
        var tokenAddress = $"fg{idHint:N}"[..44];
        var mintHash = $"fg-ledger:{idHint:N}";
        payload["nftTokenAddress"] = tokenAddress;
        payload["mintTransactionHash"] = mintHash;

        return new HolonCreateModel
        {
            Name = title,
            Description = description,
            ParentHolonId = parentAvatarId,
            ProviderName = "FieldGuideCompat",
            AssetType = CollectibleAssetType,
            TokenId = tokenAddress,
            Metadata = FlattenStringMeta(payload),
            IsPublic = false,
        };
    }

    private static Dictionary<string, string> FlattenMeta(Dictionary<string, JsonElement>? metaData)
    {
        var flat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (metaData is null || metaData.Count == 0)
        {
            flat[MetaJsonKey] = "{}";
            return flat;
        }

        flat[MetaJsonKey] = JsonSerializer.Serialize(metaData);
        foreach (var kv in metaData)
        {
            if (kv.Value.ValueKind == JsonValueKind.String)
                flat[kv.Key] = kv.Value.GetString() ?? string.Empty;
            else if (kv.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
                flat[kv.Key] = kv.Value.ToString();
        }

        return flat;
    }

    private static Dictionary<string, string> FlattenStringMeta(Dictionary<string, string> meta)
    {
        var flat = new Dictionary<string, string>(meta, StringComparer.OrdinalIgnoreCase)
        {
            [MetaJsonKey] = JsonSerializer.Serialize(meta),
        };
        return flat;
    }
}
