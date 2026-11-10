// SPDX-License-Identifier: Apache-2.0
// OASIS-shaped request/response DTOs for hitchhikers-auth (wire contract only).

namespace AZOA.WebAPI.Models;

public sealed class AvatarAuthenticateRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AvatarRegisterRequest
{
    public bool AcceptTerms { get; set; }
    public string AvatarType { get; set; } = "User";
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public sealed class AvatarGoogleAuthRequest
{
    public string? IdToken { get; set; }
    public string? idToken { get; set; }

    public string ResolvedIdToken =>
        !string.IsNullOrWhiteSpace(IdToken) ? IdToken.Trim()
        : !string.IsNullOrWhiteSpace(idToken) ? idToken.Trim()
        : string.Empty;
}

public sealed class AvatarRefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class AvatarRevokeTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>OASIS-shaped auth result hitchhikers-auth parseAvatarAuth expects.</summary>
public sealed class AvatarAuthResult
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JwtToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Level { get; set; } = "0";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
