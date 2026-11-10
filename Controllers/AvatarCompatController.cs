// SPDX-License-Identifier: Apache-2.0
// Thin OASIS /api/avatar/* facade for hitchhikers-auth + Field Guide MCP.
// Calls AZOA-native UserManager; does not copy OASIS source.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using AZOA.WebAPI.Interfaces;
using AZOA.WebAPI.Interfaces.Managers;
using AZOA.WebAPI.Models;
using AZOA.WebAPI.Models.Responses;
using AZOA.WebAPI.Services.Auth;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/avatar")]
[EnableRateLimiting("financial")]
public sealed class AvatarCompatController : ControllerBase
{
    private static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(30);

    private readonly IUserManager _users;
    private readonly IUserLoginTokenIssuer _tokens;
    private readonly IAvatarCompatRefreshTokenService _refresh;
    private readonly IGoogleIdTokenValidator _google;
    private readonly ILogger<AvatarCompatController> _logger;

    public AvatarCompatController(
        IUserManager users,
        IUserLoginTokenIssuer tokens,
        IAvatarCompatRefreshTokenService refresh,
        IGoogleIdTokenValidator google,
        ILogger<AvatarCompatController> logger)
    {
        _users = users;
        _tokens = tokens;
        _refresh = refresh;
        _google = google;
        _logger = logger;
    }

    [HttpPost("authenticate")]
    [AllowAnonymous]
    public async Task<ActionResult<AZOAResult<AvatarAuthResult>>> Authenticate(
        [FromBody] AvatarAuthenticateRequest body)
    {
        var username = (body.Username ?? string.Empty).Trim();
        var password = body.Password ?? string.Empty;
        if (username.Length == 0 || password.Length == 0)
            return BadRequest(AZOAResult<AvatarAuthResult>.Failure("Username and password are required."));

        var login = await _users.LoginAsync(new UserLoginModel
        {
            Email = username,
            Password = password,
        });
        if (login.IsError || string.IsNullOrWhiteSpace(login.Result))
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure(login.Message));

        var user = await ResolveUserFromJwtAsync(login.Result);
        if (user is null)
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure("Invalid credentials."));

        return Ok(AZOAResult<AvatarAuthResult>.Success(
            BuildAuthResult(user, login.Result),
            "Avatar Successfully Authenticated."));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AZOAResult<object>>> Register([FromBody] AvatarRegisterRequest body)
    {
        if (!body.AcceptTerms)
            return BadRequest(AZOAResult<object>.Failure("AcceptTerms must be true."));
        if (!string.Equals(body.Password, body.ConfirmPassword, StringComparison.Ordinal))
            return BadRequest(AZOAResult<object>.Failure("Password and ConfirmPassword must match."));
        if (string.IsNullOrWhiteSpace(body.Password) || body.Password.Length < 8)
            return BadRequest(AZOAResult<object>.Failure("Use at least 8 characters."));

        var result = await _users.RegisterAsync(new UserRegisterModel
        {
            Email = body.Email,
            Username = body.Username,
            Password = body.Password,
            FirstName = body.FirstName,
            LastName = body.LastName,
            Title = body.Title,
        });
        if (result.IsError || result.Result is null)
            return BadRequest(AZOAResult<object>.Failure(result.Message));

        var profile = new
        {
            id = result.Result.Id,
            username = result.Result.Username,
            email = result.Result.Email,
            firstName = result.Result.FirstName,
            lastName = result.Result.LastName,
        };
        return Ok(AZOAResult<object>.Success(profile, "Avatar registered."));
    }

    [HttpPost("auth/google")]
    [AllowAnonymous]
    public async Task<ActionResult<AZOAResult<AvatarAuthResult>>> AuthGoogle(
        [FromBody] AvatarGoogleAuthRequest body,
        CancellationToken ct)
    {
        var idToken = body.ResolvedIdToken;
        if (idToken.Length == 0)
            return BadRequest(AZOAResult<AvatarAuthResult>.Failure("idToken is required."));

        var subject = await _google.ValidateAsync(idToken, ct);
        if (subject is null)
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure("Google sign-in could not be verified."));

        var user = await FindOrCreateGoogleUserAsync(subject);
        if (user is null)
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure("Google account could not be linked."));

        var jwt = _tokens.IssueLoginToken(user);
        return Ok(AZOAResult<AvatarAuthResult>.Success(
            BuildAuthResult(user, jwt),
            "Avatar Successfully Authenticated."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AZOAResult<AvatarAuthResult>>> RefreshToken(
        [FromBody] AvatarRefreshTokenRequest body)
    {
        var refresh = (body.RefreshToken ?? string.Empty).Trim();
        if (refresh.Length == 0)
            return BadRequest(AZOAResult<AvatarAuthResult>.Failure("refreshToken is required."));

        if (!_refresh.TryConsume(refresh, out var userId))
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure("Invalid refresh token."));

        var loaded = await _users.GetAsync(userId);
        if (loaded.IsError || loaded.Result is null || !loaded.Result.IsActive)
            return Unauthorized(AZOAResult<AvatarAuthResult>.Failure("Invalid refresh token."));

        var jwt = _tokens.IssueLoginToken(loaded.Result);
        return Ok(AZOAResult<AvatarAuthResult>.Success(
            BuildAuthResult(loaded.Result, jwt),
            "Token refreshed."));
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<AZOAResult<object>>> RevokeToken([FromBody] AvatarRevokeTokenRequest body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Unauthorized."));

        var token = (body.Token ?? string.Empty).Trim();
        if (token.Length == 0)
            return BadRequest(AZOAResult<object>.Failure("token is required."));

        _refresh.Revoke(token, userId.Value);
        await _users.LogoutEverywhereAsync(userId.Value, HttpContext.RequestAborted);
        return Ok(AZOAResult<object>.Success(new { }, "Token revoked."));
    }

    [HttpGet("get-logged-in-avatar")]
    [Authorize]
    public async Task<ActionResult<AZOAResult<object>>> GetLoggedInAvatar()
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Unauthorized."));

        var loaded = await _users.GetAsync(userId.Value);
        if (loaded.IsError || loaded.Result is null)
            return NotFound(AZOAResult<object>.Failure("Avatar not found."));

        var u = loaded.Result;
        var avatar = new
        {
            id = u.Id,
            username = u.Username,
            email = u.Email,
            firstName = u.FirstName,
            lastName = u.LastName,
            title = u.Title,
            isActive = u.IsActive,
        };
        return Ok(AZOAResult<object>.Success(avatar, "Success"));
    }

    private AvatarAuthResult BuildAuthResult(IUser user, string jwt)
    {
        var refresh = _refresh.Issue(user.Id, RefreshLifetime);
        return new AvatarAuthResult
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            JwtToken = jwt,
            RefreshToken = refresh,
            Level = "0",
        };
    }

    private async Task<IUser?> FindOrCreateGoogleUserAsync(GoogleIdTokenSubject subject)
    {
        var all = await _users.GetAllAsync();
        if (!all.IsError && all.Result is not null)
        {
            var existing = all.Result.FirstOrDefault(u =>
                string.Equals(u.Email, subject.Email, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
                return existing;
        }

        var handle = $"g_{subject.Subject[..Math.Min(12, subject.Subject.Length)]}";
        var names = (subject.Name ?? string.Empty).Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var registered = await _users.RegisterAsync(new UserRegisterModel
        {
            Email = subject.Email,
            Username = handle,
            Password = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "Aa1!",
            FirstName = names.Length > 0 ? names[0] : "Google",
            LastName = names.Length > 1 ? names[1] : "User",
        });
        if (!registered.IsError && registered.Result is not null)
            return registered.Result;

        // Race: email already taken between probe and register — re-read.
        all = await _users.GetAllAsync();
        if (!all.IsError && all.Result is not null)
        {
            return all.Result.FirstOrDefault(u =>
                string.Equals(u.Email, subject.Email, StringComparison.OrdinalIgnoreCase));
        }

        _logger.LogWarning("Google find-or-create failed for {Email}: {Message}", subject.Email, registered.Message);
        return null;
    }

    private async Task<IUser?> ResolveUserFromJwtAsync(string jwt)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var id = token.Claims.FirstOrDefault(c => c.Type == "id")?.Value
                     ?? token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (!Guid.TryParse(id, out var userId))
                return null;
            var loaded = await _users.GetAsync(userId);
            return loaded.IsError ? null : loaded.Result;
        }
        catch
        {
            return null;
        }
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
