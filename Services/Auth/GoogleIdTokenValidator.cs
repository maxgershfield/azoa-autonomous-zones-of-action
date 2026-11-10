// SPDX-License-Identifier: Apache-2.0
// GIS id_token validation for POST /api/avatar/auth/google (hitchhikers-auth).

using System.Text.Json;

namespace AZOA.WebAPI.Services.Auth;

public interface IGoogleIdTokenValidator
{
    Task<GoogleIdTokenSubject?> ValidateAsync(string idToken, CancellationToken ct = default);
}

public sealed record GoogleIdTokenSubject(string Subject, string Email, string? Name, bool EmailVerified);

/// <summary>
/// Validates a Google Sign-In id_token via Google's tokeninfo endpoint.
/// Audience must match AvatarCompat:GoogleClientId (Field Guide GIS client).
/// </summary>
public sealed class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleIdTokenValidator> _logger;

    public GoogleIdTokenValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GoogleIdTokenValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GoogleIdTokenSubject?> ValidateAsync(string idToken, CancellationToken ct = default)
    {
        var expectedAudience = _configuration["AvatarCompat:GoogleClientId"]
            ?? _configuration["Authentication:Google:ClientId"];
        if (string.IsNullOrWhiteSpace(expectedAudience))
        {
            _logger.LogWarning("AvatarCompat GoogleClientId is not configured; refusing id_token.");
            return null;
        }

        var client = _httpClientFactory.CreateClient(nameof(GoogleIdTokenValidator));
        using var response = await client.GetAsync(
            $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(idToken)}",
            ct);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        var aud = root.TryGetProperty("aud", out var audEl) ? audEl.GetString() : null;
        if (!string.Equals(aud, expectedAudience, StringComparison.Ordinal))
            return null;

        var sub = root.TryGetProperty("sub", out var subEl) ? subEl.GetString() : null;
        var email = root.TryGetProperty("email", out var emailEl) ? emailEl.GetString() : null;
        if (string.IsNullOrWhiteSpace(sub) || string.IsNullOrWhiteSpace(email))
            return null;

        var emailVerified = root.TryGetProperty("email_verified", out var verifiedEl)
            && (verifiedEl.ValueKind == JsonValueKind.True
                || string.Equals(verifiedEl.GetString(), "true", StringComparison.OrdinalIgnoreCase));
        if (!emailVerified)
            return null;

        var name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
        return new GoogleIdTokenSubject(sub, email.Trim().ToLowerInvariant(), name, emailVerified);
    }
}
