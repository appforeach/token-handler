using AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AppForeach.TokenHandler.BackgroundWorker.Tests;

public class ExpiringTokensRefreshWorkerTests
{
    [Fact]
    public void Constructor_StoresDependencies_WhenValidParametersProvided()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromMinutes(1) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();

        // Act
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenPollingIntervalIsZero()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.Zero });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await worker.StartAsync(cts.Token));
        Assert.Equal($"{nameof(ExpiringTokensRefreshWorker)}:PollingInterval must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsInvalidOperationException_WhenPollingIntervalIsNegative()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromSeconds(-1) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await worker.StartAsync(cts.Token));
        Assert.Equal($"{nameof(ExpiringTokensRefreshWorker)}:PollingInterval must be greater than zero.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_CallsHandeAsync_WhenPollingIntervalIsValid()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromMilliseconds(10) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        mockService
            .Setup(s => s.HandeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => cts.Cancel());

        // Act
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mockService.Verify(s => s.HandeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_StopsLoop_WhenCancellationRequested()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromMilliseconds(10) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        mockService
            .Setup(s => s.HandeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Assert
        mockService.Verify(s => s.HandeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_CallsHandeAsyncMultipleTimes_WhenPollingIntervalElapses()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromMilliseconds(10) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        var callCount = 0;
        mockService
            .Setup(s => s.HandeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                callCount++;
                if (callCount >= 3)
                {
                    cts.Cancel();
                }
            });

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(150);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callCount >= 3, $"Expected at least 3 calls, but got {callCount}");
        mockService.Verify(s => s.HandeAsync(It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task ExecuteAsync_PassesCancellationToken_ToHandeAsync()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = TimeSpan.FromMilliseconds(10) });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        CancellationToken? receivedToken = null;
        mockService
            .Setup(s => s.HandeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<CancellationToken>(token =>
            {
                receivedToken = token;
            });

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        try
        {
            await worker.StopAsync(CancellationToken.None);
        }
        catch (TaskCanceledException)
        {
            // Expected when cancellation occurs during Task.Delay
        }

        // Assert
        Assert.NotNull(receivedToken);
        mockService.Verify(s => s.HandeAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task ExecuteAsync_DelaysForPollingInterval_BetweenCalls()
    {
        // Arrange
        var mockService = new Mock<IExpiringTokensRefreshService>();
        var pollingInterval = TimeSpan.FromMilliseconds(50);
        var options = Options.Create(new ExpiringTokensRefreshWorkerOptions { PollingInterval = pollingInterval });
        var mockLogger = new Mock<ILogger<ExpiringTokensRefreshWorker>>();
        var worker = new ExpiringTokensRefreshWorker(mockService.Object, options, mockLogger.Object);
        using var cts = new CancellationTokenSource();

        var callTimes = new List<DateTimeOffset>();
        mockService
            .Setup(s => s.HandeAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() =>
            {
                callTimes.Add(DateTimeOffset.UtcNow);
                if (callTimes.Count >= 2)
                {
                    cts.Cancel();
                }
            });

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.True(callTimes.Count >= 2, $"Expected at least 2 calls, but got {callTimes.Count}");
        if (callTimes.Count >= 2)
        {
            var timeBetweenCalls = callTimes[1] - callTimes[0];
            Assert.True(timeBetweenCalls >= pollingInterval,
                $"Time between calls ({timeBetweenCalls.TotalMilliseconds}ms) should be >= polling interval ({pollingInterval.TotalMilliseconds}ms)");
        }
    }
}
