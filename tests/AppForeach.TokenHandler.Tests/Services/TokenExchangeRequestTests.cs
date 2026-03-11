using AppForeach.TokenHandler.Services;

namespace AppForeach.TokenHandler.Tests.Services;
public class TokenExchangeRequestTests
{
    [Fact]
    public void SubjectTokenType_DefaultsToAccessTokenType()
    {
        var request = new TokenExchangeRequest { SubjectToken = "test-token" };

        Assert.Equal(TokenExchangeConstants.AccessTokenType, request.SubjectTokenType);
    }

    [Fact]
    public void RequestedTokenType_DefaultsToAccessTokenType()
    {
        var request = new TokenExchangeRequest { SubjectToken = "test-token" };

        Assert.Equal(TokenExchangeConstants.AccessTokenType, request.RequestedTokenType);
    }

    [Fact]
    public void Resource_DefaultsToNull()
    {
        var request = new TokenExchangeRequest { SubjectToken = "test-token" };

        Assert.Null(request.Resource);
    }

    [Fact]
    public void Audience_DefaultsToNull()
    {
        var request = new TokenExchangeRequest { SubjectToken = "test-token" };

        Assert.Null(request.Audience);
    }

    [Fact]
    public void Scopes_DefaultsToNull()
    {
        var request = new TokenExchangeRequest { SubjectToken = "test-token" };

        Assert.Null(request.Scopes);
    }

    [Fact]
    public void AllProperties_CanBeSetToCustomValues()
    {
        var scopes = new[] { "scope1", "scope2" };

        var request = new TokenExchangeRequest
        {
            SubjectToken = "custom-subject-token",
            SubjectTokenType = TokenExchangeConstants.IdTokenType,
            Resource = "https://api.example.com",
            Audience = "client-id-123",
            Scopes = scopes,
            RequestedTokenType = TokenExchangeConstants.RefreshTokenType
        };

        Assert.Equal("custom-subject-token", request.SubjectToken);
        Assert.Equal(TokenExchangeConstants.IdTokenType, request.SubjectTokenType);
        Assert.Equal("https://api.example.com", request.Resource);
        Assert.Equal("client-id-123", request.Audience);
        Assert.Equal(scopes, request.Scopes);
        Assert.Equal(TokenExchangeConstants.RefreshTokenType, request.RequestedTokenType);
    }
}

public class TokenExchangeConstantsTests
{
    [Fact]
    public void GrantType_HasCorrectRfc8693Value()
    {
        Assert.Equal("urn:ietf:params:oauth:grant-type:token-exchange", TokenExchangeConstants.GrantType);
    }

    [Fact]
    public void AccessTokenType_HasCorrectRfc8693Value()
    {
        Assert.Equal("urn:ietf:params:oauth:token-type:access_token", TokenExchangeConstants.AccessTokenType);
    }

    [Fact]
    public void RefreshTokenType_HasCorrectRfc8693Value()
    {
        Assert.Equal("urn:ietf:params:oauth:token-type:refresh_token", TokenExchangeConstants.RefreshTokenType);
    }

    [Fact]
    public void IdTokenType_HasCorrectRfc8693Value()
    {
        Assert.Equal("urn:ietf:params:oauth:token-type:id_token", TokenExchangeConstants.IdTokenType);
    }
}