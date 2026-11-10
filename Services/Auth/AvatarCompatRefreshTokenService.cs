// SPDX-License-Identifier: Apache-2.0
// Field Guide / hitchhikers-auth compat: opaque refresh tokens for /api/avatar/*.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace AZOA.WebAPI.Services.Auth;

public interface IAvatarCompatRefreshTokenService
{
    string Issue(Guid userId, TimeSpan lifetime);
    bool TryConsume(string refreshToken, out Guid userId);
    bool Revoke(string refreshToken, Guid userId);
    void RevokeAllForUser(Guid userId);
}

/// <summary>
/// In-process refresh store for the OASIS-shaped avatar facade.
/// Single-node staging is enough for Field Guide cutover; multi-instance
/// deployments must replace this with a Surreal-backed store.
/// </summary>
public sealed class AvatarCompatRefreshTokenService : IAvatarCompatRefreshTokenService
{
    private readonly ConcurrentDictionary<string, Entry> _byHash = new(StringComparer.Ordinal);

    private sealed record Entry(Guid UserId, DateTimeOffset ExpiresAt);

    public string Issue(Guid userId, TimeSpan lifetime)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var hash = Hash(raw);
        _byHash[hash] = new Entry(userId, DateTimeOffset.UtcNow.Add(lifetime));
        return raw;
    }

    public bool TryConsume(string refreshToken, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var hash = Hash(refreshToken.Trim());
        if (!_byHash.TryGetValue(hash, out var entry))
            return false;
        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _byHash.TryRemove(hash, out _);
            return false;
        }

        // Rotate: one-time use.
        _byHash.TryRemove(hash, out _);
        userId = entry.UserId;
        return true;
    }

    public bool Revoke(string refreshToken, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;
        var hash = Hash(refreshToken.Trim());
        if (!_byHash.TryGetValue(hash, out var entry))
            return false;
        if (entry.UserId != userId)
            return false;
        return _byHash.TryRemove(hash, out _);
    }

    public void RevokeAllForUser(Guid userId)
    {
        foreach (var pair in _byHash)
        {
            if (pair.Value.UserId == userId)
                _byHash.TryRemove(pair.Key, out _);
        }
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}
