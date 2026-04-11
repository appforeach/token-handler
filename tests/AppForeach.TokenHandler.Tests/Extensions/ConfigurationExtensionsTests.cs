using AppForeach.TokenHandler.Controllers;
using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Middleware;
using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppForeach.TokenHandler.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void AddTokenExchangeDelegatingHandler_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTokenExchangeDelegatingHandler();

        // Assert
        Assert.Same(services, result);

        // Verify service registrations
        Assert.Contains(services, s => s.ServiceType == typeof(ITokenExchangeService) && s.ImplementationType == typeof(TokenExchangeService));
        Assert.Contains(services, s => s.ServiceType == typeof(TokenExchangeDelegatingHandler));
        Assert.Contains(services, s => s.ServiceType == typeof(IHttpContextAccessor));
    }

    [Fact]
    public void AddTokenHandler_WithNullOptions_RegistersServicesWithDefaultOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddTokenHandler(null!);

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetService<IOptions<TokenHandlerOptions>>();
        Assert.NotNull(options);
    }

    [Fact]
    public void AddTokenHandler_WithCustomOptions_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedAuthority = "https://custom.authority.com";
        var expectedClientId = "custom-client-id";
        var expectedClientSecret = "custom-secret";
        var expectedRealm = "custom-realm";

        // Act
        var result = services.AddTokenHandler(options =>
        {
            options.Authority = expectedAuthority;
            options.ClientId = expectedClientId;
            options.ClientSecret = expectedClientSecret;
            options.Realm = expectedRealm;
        });

        // Assert
        Assert.Same(services, result);
        var serviceProvider = services.BuildServiceProvider();

        var configuredOptions = serviceProvider.GetRequiredService<IOptions<TokenHandlerOptions>>().Value;
        Assert.Equal(expectedAuthority, configuredOptions.Authority);
        Assert.Equal(expectedClientId, configuredOptions.ClientId);
        Assert.Equal(expectedClientSecret, configuredOptions.ClientSecret);
        Assert.Equal(expectedRealm, configuredOptions.Realm);
    }

    [Fact]
    public void AddTokenHandler_RegistersMemoryCacheServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var hybridCache = serviceProvider.GetService<HybridCache>();
        Assert.NotNull(hybridCache);
    }

    [Fact]
    public void AddTokenHandler_RegistersHttpContextAccessor()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        Assert.NotNull(httpContextAccessor);
    }

    [Fact]
    public void AddTokenHandler_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
        var client = httpClientFactory.CreateClient("TokenExchange");
        Assert.NotNull(client);
    }

    [Fact]
    public void AddTokenHandler_RegistersTokenExchangeService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var tokenExchangeService = serviceProvider.GetService<ITokenExchangeService>();
        Assert.NotNull(tokenExchangeService);
        Assert.IsType<TokenExchangeService>(tokenExchangeService);
    }

    [Fact]
    public void AddTokenHandler_RegistersTokenExchangeDelegatingHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var delegatingHandler = serviceProvider.GetService<TokenExchangeDelegatingHandler>();
        Assert.NotNull(delegatingHandler);
    }

    [Fact]
    public void AddTokenHandler_RegistersAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options =>
        {
            options.Authority = "https://test.authority.com";
            options.ClientId = "test-client";
            options.ClientSecret = "test-secret";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var authenticationSchemeProvider = serviceProvider.GetService<IAuthenticationSchemeProvider>();
        Assert.NotNull(authenticationSchemeProvider);
    }

    [Fact]
    public void AddTokenHandler_RegistersControllers()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        Assert.Contains(services, s => s.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Mvc.MvcOptions>));
    }

    [Fact]
    public void UseTokenHandler_RegistersMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());

        // Act
        var result = appBuilder.UseTokenHandler();

        // Assert
        Assert.Same(appBuilder, result);
    }

    [Fact]
    public void AddTokenExchangeDelegatingHandler_RegistersServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenExchangeDelegatingHandler();

        // Assert
        var tokenExchangeServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenExchangeService));
        Assert.NotNull(tokenExchangeServiceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, tokenExchangeServiceDescriptor.Lifetime);

        var delegatingHandlerDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TokenExchangeDelegatingHandler));
        Assert.NotNull(delegatingHandlerDescriptor);
        Assert.Equal(ServiceLifetime.Transient, delegatingHandlerDescriptor.Lifetime);
    }

    [Fact]
    public void AddTokenHandler_RegistersServicesWithCorrectLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        var tokenExchangeServiceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ITokenExchangeService));
        Assert.NotNull(tokenExchangeServiceDescriptor);
        Assert.Equal(ServiceLifetime.Transient, tokenExchangeServiceDescriptor.Lifetime);

        var delegatingHandlerDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TokenExchangeDelegatingHandler));
        Assert.NotNull(delegatingHandlerDescriptor);
        Assert.Equal(ServiceLifetime.Transient, delegatingHandlerDescriptor.Lifetime);
    }

    [Fact]
    public void AddTokenHandler_RegistersDistributedMemoryCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddTokenHandler(options => { });

        // Assert
        Assert.Contains(services, s => s.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache));
    }
}
