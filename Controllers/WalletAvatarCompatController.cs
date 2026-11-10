// SPDX-License-Identifier: Apache-2.0
// OASIS /api/wallet/avatar/* facade over AZOA WalletManager.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AZOA.WebAPI.Interfaces;
using AZOA.WebAPI.Interfaces.Managers;
using AZOA.WebAPI.Models.Requests;
using AZOA.WebAPI.Models.Responses;

namespace AZOA.WebAPI.Controllers;

[ApiController]
[Route("api/wallet/avatar")]
[Authorize]
public sealed class WalletAvatarCompatController : ControllerBase
{
    private readonly IWalletManager _wallets;

    public WalletAvatarCompatController(IWalletManager wallets)
    {
        _wallets = wallets;
    }

    [HttpGet("{avatarId}/wallets/{loadBalance}/{loadNfts}")]
    public async Task<ActionResult<AZOAResult<object>>> ListWallets(
        string avatarId,
        bool loadBalance,
        bool loadNfts)
    {
        _ = loadNfts;
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(avatarId, out var avatar) || avatar != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("Cannot load wallets for another avatar."));

        var query = await _wallets.QueryAsync(new WalletQueryRequest(), userId.Value);
        if (query.IsError || query.Result is null)
            return Ok(AZOAResult<object>.Failure(query.Message));

        var buckets = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
        foreach (var wallet in query.Result)
        {
            var provider = MapProvider(wallet.ChainType);
            if (!buckets.TryGetValue(provider, out var list))
            {
                list = new List<object>();
                buckets[provider] = list;
            }

            decimal? balance = null;
            if (loadBalance)
            {
                var portfolio = await _wallets.GetPortfolioAsync(wallet.Id, userId.Value);
                if (!portfolio.IsError && portfolio.Result is not null)
                    balance = portfolio.Result.Balance;
            }

            list.Add(MapWallet(wallet, balance));
        }

        return Ok(AZOAResult<object>.Success(buckets));
    }

    [HttpGet("{avatarId}/default-wallet")]
    public async Task<ActionResult<AZOAResult<object>>> DefaultWallet(
        string avatarId,
        [FromQuery] string? providerType = null)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(avatarId, out var avatar) || avatar != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("Cannot load wallets for another avatar."));

        var query = await _wallets.QueryAsync(new WalletQueryRequest(), userId.Value);
        if (query.IsError || query.Result is null)
            return NotFound(AZOAResult<object>.Failure(query.Message));

        var wantChain = MapChain(providerType);
        var match = query.Result
            .Where(w => wantChain is null
                        || string.Equals(w.ChainType, wantChain, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(MapProvider(w.ChainType), providerType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(w => w.IsDefault)
            .FirstOrDefault();

        if (match is null)
            return NotFound(AZOAResult<object>.Failure("Default wallet not found."));

        decimal? balance = null;
        var portfolio = await _wallets.GetPortfolioAsync(match.Id, userId.Value);
        if (!portfolio.IsError && portfolio.Result is not null)
            balance = portfolio.Result.Balance;

        return Ok(AZOAResult<object>.Success(MapWallet(match, balance)));
    }

    [HttpGet("{avatarId}/wallet/{walletId}/tokens")]
    public async Task<ActionResult<AZOAResult<object>>> Tokens(string avatarId, string walletId)
    {
        var userId = GetUserIdFromClaims();
        if (userId is null)
            return Unauthorized(AZOAResult<object>.Failure("Invalid token."));
        if (!Guid.TryParse(avatarId, out var avatar) || avatar != userId.Value)
            return StatusCode(StatusCodes.Status403Forbidden,
                AZOAResult<object>.Failure("Cannot load tokens for another avatar."));
        if (!Guid.TryParse(walletId, out var wid))
            return BadRequest(AZOAResult<object>.Failure("walletId must be a GUID."));

        var wallet = await _wallets.GetAsync(wid, userId.Value);
        if (wallet.IsError || wallet.Result is null)
            return NotFound(AZOAResult<object>.Failure(wallet.Message));

        var portfolio = await _wallets.GetPortfolioAsync(wid, userId.Value);
        var balance = portfolio.IsError || portfolio.Result is null ? 0m : portfolio.Result.Balance;
        var symbol = portfolio.Result?.Symbol;
        if (string.IsNullOrWhiteSpace(symbol))
            symbol = string.Equals(wallet.Result.ChainType, "Solana", StringComparison.OrdinalIgnoreCase)
                ? "SOL"
                : wallet.Result.ChainType;

        var tokens = new object[]
        {
            new
            {
                symbol,
                Symbol = symbol,
                balance,
                Balance = balance,
                isNative = true,
                IsNative = true,
            },
        };
        return Ok(AZOAResult<object>.Success(tokens));
    }

    private static object MapWallet(IWallet wallet, decimal? balance) => new
    {
        id = wallet.Id,
        Id = wallet.Id,
        walletId = wallet.Id,
        WalletId = wallet.Id,
        walletAddress = wallet.Address,
        WalletAddress = wallet.Address,
        address = wallet.Address,
        Address = wallet.Address,
        publicKey = wallet.PublicKey ?? wallet.Address,
        PublicKey = wallet.PublicKey ?? wallet.Address,
        isDefaultWallet = wallet.IsDefault,
        IsDefaultWallet = wallet.IsDefault,
        balance,
        Balance = balance,
        chainType = wallet.ChainType,
        ChainType = wallet.ChainType,
    };

    private static string MapProvider(string chainType) =>
        chainType?.Trim().ToLowerInvariant() switch
        {
            "solana" => "SolanaOASIS",
            "ethereum" => "EthereumOASIS",
            "algorand" => "AlgorandOASIS",
            _ => string.IsNullOrWhiteSpace(chainType) ? "Unknown" : $"{chainType}OASIS",
        };

    private static string? MapChain(string? providerType)
    {
        if (string.IsNullOrWhiteSpace(providerType))
            return "Solana";
        var p = providerType.Trim();
        if (p.Contains("Solana", StringComparison.OrdinalIgnoreCase))
            return "Solana";
        if (p.Contains("Ethereum", StringComparison.OrdinalIgnoreCase))
            return "Ethereum";
        if (p.Contains("Algorand", StringComparison.OrdinalIgnoreCase))
            return "Algorand";
        return null;
    }

    private Guid? GetUserIdFromClaims()
    {
        var raw = User?.FindFirst("id")?.Value
                  ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
