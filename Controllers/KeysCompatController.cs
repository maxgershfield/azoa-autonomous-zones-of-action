// SPDX-License-Identifier: Apache-2.0
// OASIS /api/keys/* wallet provision facade → AZOA BootstrapWalletAsync(Solana).

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Interfaces.Managers;
using AZOA.WebAPI.Models.FieldGuide;
using AZOA.WebAPI.Models.Requests;
using AZOA.WebAPI.Models.Responses;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/keys")]
[Authorize]
public sealed class KeysCompatController : ControllerBase
{
    private readonly IWalletManager _wallets;
    private readonly IUserManager _users;

    public KeysCompatController(IWalletManager wallets, IUserManager users)
    {
        _wallets = wallets;
        _users = users;
    }

    [HttpPost("generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_id")]
    public async Task<ActionResult<AZOAResult<object>>> GenerateByAvatarId(
        [FromBody] FieldGuideKeysProvisionBody body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(body.AvatarID, out var avatarId) || avatarId != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("AvatarID must match the authenticated user."));

        return await BootstrapSolanaAsync(userId.Value);
    }

    [HttpPost("generate_keypair_with_wallet_address_and_link_provider_keys_to_avatar_by_email")]
    public async Task<ActionResult<AZOAResult<object>>> GenerateByEmail(
        [FromBody] FieldGuideKeysProvisionBody body)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));

        var email = (body.AvatarEmail ?? string.Empty).Trim();
        if (email.Length == 0)
            return BadRequest(AZOAResult<object>.Failure("AvatarEmail is required."));

        var user = await _users.GetAsync(userId.Value);
        if (user.IsError || user.Result is null)
            return Unauthorized(AZOAResult<object>.Failure(user.Message));
        if (!string.Equals(user.Result.Email, email, StringComparison.OrdinalIgnoreCase))
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("AvatarEmail must match the authenticated user."));

        return await BootstrapSolanaAsync(userId.Value);
    }

    private async Task<ActionResult<AZOAResult<object>>> BootstrapSolanaAsync(Guid userId)
    {
        var result = await _wallets.BootstrapWalletAsync(new WalletGenerateRequest
        {
            ChainType = "Solana",
            Label = "Field Guide Solana",
            IsDefault = true,
        }, userId);

        if (result.IsError || result.Result is null)
            return BadRequest(AZOAResult<object>.Failure(result.Message));

        var wallet = result.Result;
        return Ok(AZOAResult<object>.Success(new
        {
            walletAddress = wallet.Address,
            WalletAddress = wallet.Address,
            address = wallet.Address,
            Address = wallet.Address,
            publicKey = wallet.PublicKey ?? wallet.Address,
            PublicKey = wallet.PublicKey ?? wallet.Address,
            id = wallet.Id,
            Id = wallet.Id,
            providerType = "SolanaOASIS",
            ProviderType = "SolanaOASIS",
        }, "Solana wallet provisioned."));
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
