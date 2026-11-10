using System.IdentityModel.Tokens.Jwt;
using AZOA.WebAPI.Interfaces;
using AZOA.WebAPI.Services.Auth;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AZOA.WebAPI.Tests.Auth;

public class AvatarCompatJwtIdClaimTests
{
    [Fact]
    public void BuildLoginToken_IncludesIdClaimMatchingUserId()
    {
        var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var user = new Mock<IUser>();
        user.SetupGet(u => u.Id).Returns(userId);
        user.SetupGet(u => u.Email).Returns("guide@example.com");
        user.SetupGet(u => u.Username).Returns("guide");
        user.SetupGet(u => u.DappRole).Returns("dapp:user");

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "avatar-compat-test-key-min-32-chars!!",
                ["Jwt:Issuer"] = "azoa-test",
                ["Jwt:Audience"] = "azoa-test",
            })
            .Build();

        var jwt = UserLoginTokenIssuer.BuildLoginToken(configuration, user.Object);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

        Assert.Equal(userId.ToString(), token.Claims.First(c => c.Type == "id").Value);
        Assert.Equal(userId.ToString(), token.Claims.First(c => c.Type == "sub").Value);
    }
}
