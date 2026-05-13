using FluentAssertions;
using Max.Bot.Configuration;
using Xunit;

namespace Max.Bot.Tests.Unit.Configuration;

public class MaxBotClientOptionsTests
{
    [Fact]
    public void MaxBotClientOptions_ShouldHaveDefaultValues()
    {
        // Act
        var options = new MaxBotClientOptions();

        // Assert
        options.BaseUrl.Should().BeEmpty();
        options.Timeout.Should().Be(TimeSpan.FromSeconds(100));
        options.RetryCount.Should().Be(3);
        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(1));
        options.MaxRetryDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.EnableDetailedLogging.Should().BeFalse();
    }

    [Fact]
    public void MaxBotClientOptions_ShouldValidate_WithValidValues()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = TimeSpan.FromSeconds(60),
            RetryCount = 5,
            RetryBaseDelay = TimeSpan.FromSeconds(2),
            MaxRetryDelay = TimeSpan.FromSeconds(60),
            EnableDetailedLogging = true
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenBaseUrlIsNullOrEmpty(string? baseUrl)
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = baseUrl!
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(MaxBotClientOptions.BaseUrl));
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("not-a-uri")]
    [InlineData("/relative/path")]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenBaseUrlIsNotValidUri(string baseUrl)
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = baseUrl
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(MaxBotClientOptions.BaseUrl));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldAllowInfiniteTimeout()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = Timeout.InfiniteTimeSpan
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenTimeoutIsZero()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = TimeSpan.Zero
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxBotClientOptions.Timeout));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenTimeoutIsNegative()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = TimeSpan.FromSeconds(-1)
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxBotClientOptions.Timeout));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenRetryCountIsNegative()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            RetryCount = -1
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxBotClientOptions.RetryCount));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldAllowZeroRetryCount()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            RetryCount = 0
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenRetryBaseDelayIsZero()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            RetryBaseDelay = TimeSpan.Zero
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxBotClientOptions.RetryBaseDelay));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenMaxRetryDelayIsZero()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            MaxRetryDelay = TimeSpan.Zero
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxBotClientOptions.MaxRetryDelay));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldThrow_WhenMaxRetryDelayIsLessThanRetryBaseDelay()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            RetryBaseDelay = TimeSpan.FromSeconds(5),
            MaxRetryDelay = TimeSpan.FromSeconds(2)
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName(nameof(MaxBotClientOptions.MaxRetryDelay));
    }

    [Fact]
    public void MaxBotClientOptions_Validate_ShouldAllow_MaxRetryDelayEqualToRetryBaseDelay()
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            RetryBaseDelay = TimeSpan.FromSeconds(5),
            MaxRetryDelay = TimeSpan.FromSeconds(5)
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("https://api.max.ru/bot")]
    [InlineData("https://api.max.ru/bot/")]
    [InlineData("http://localhost:8080/api")]
    [InlineData("https://subdomain.example.com/v1")]
    public void MaxBotClientOptions_Validate_ShouldAccept_ValidAbsoluteUris(string baseUrl)
    {
        // Arrange
        var options = new MaxBotClientOptions
        {
            BaseUrl = baseUrl
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}

