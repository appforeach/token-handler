using AppForeach.TokenHandler.Extensions;

namespace AppForeach.TokenHandler.Tests.Extensions;

public class TokenHandlerOptionsTests
{
    [Fact]
    public void Default_ReturnsNonNullInstance()
    {
        // Act
        var result = TokenHandlerOptions.Default;

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Default_Authority_HasExpectedValue()
    {
        // Act
        var result = TokenHandlerOptions.Default;

        // Assert
        Assert.Equal("http://localhost:8080/realms/poc", result.Authority);
    }

    [Fact]
    public void Default_ClientId_HasExpectedValue()
    {
        // Act
        var result = TokenHandlerOptions.Default;

        // Assert
        Assert.Equal("poc-api", result.ClientId);
    }

    [Fact]
    public void Default_ClientSecret_HasExpectedValue()
    {
        // Act
        var result = TokenHandlerOptions.Default;

        // Assert
        Assert.Equal("your-client-secret-here", result.ClientSecret);
    }

    [Fact]
    public void Default_Realm_HasExpectedValue()
    {
        // Act
        var result = TokenHandlerOptions.Default;

        // Assert
        Assert.Equal("poc", result.Realm);
    }

    [Fact]
    public void Default_ReturnsNewInstanceEachTime()
    {
        // Act
        var result1 = TokenHandlerOptions.Default;
        var result2 = TokenHandlerOptions.Default;

        // Assert
        Assert.NotSame(result1, result2);
    }
}
