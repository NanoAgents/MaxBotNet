using FluentAssertions;
using Max.Bot.Configuration;

namespace Max.Bot.Tests.Unit.Configuration;

public class HandlerTimeoutOptionsTests
{
    [Fact]
    public void UpdateHandlingOptions_Validate_ShouldAllowInfiniteHandlerTimeout()
    {
        var options = new UpdateHandlingOptions
        {
            HandlerTimeout = Timeout.InfiniteTimeSpan
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void MaxWebhookOptions_Validate_ShouldAllowInfiniteHandlerTimeout()
    {
        var options = new MaxWebhookOptions
        {
            HandlerTimeout = Timeout.InfiniteTimeSpan
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateHandlingOptions_Validate_ShouldThrow_WhenHandlerTimeoutExceedsCancelAfterRange()
    {
        var options = new UpdateHandlingOptions
        {
            HandlerTimeout = TimeSpan.MaxValue
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(UpdateHandlingOptions.HandlerTimeout));
    }

    [Fact]
    public void UpdateHandlingOptions_Validate_ShouldAllowMaxCancelAfterTimeout()
    {
        var options = new UpdateHandlingOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(int.MaxValue)
        };

        var act = () => options.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateHandlingOptions_Validate_ShouldThrow_WhenHandlerTimeoutIsOtherNegativeValue()
    {
        var options = new UpdateHandlingOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(-2)
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(UpdateHandlingOptions.HandlerTimeout));
    }

    [Fact]
    public void MaxWebhookOptions_Validate_ShouldThrow_WhenHandlerTimeoutIsOtherNegativeValue()
    {
        var options = new MaxWebhookOptions
        {
            HandlerTimeout = TimeSpan.FromMilliseconds(-2)
        };

        var act = () => options.Validate();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName(nameof(MaxWebhookOptions.HandlerTimeout));
    }
}
