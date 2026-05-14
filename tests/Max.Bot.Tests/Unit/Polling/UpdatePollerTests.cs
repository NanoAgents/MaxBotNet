using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Polling;

public class UpdatePollerTests
{
    [Fact]
    public async Task StartAsync_ShouldUseRawTokenAuthorization_ForUpdatesRequest()
    {
        var options = new MaxBotOptions
        {
            Token = "test-token-123",
            BaseUrl = "https://api.max.ru/bot"
        };

        var updatesRequestSeen = new TaskCompletionSource<MaxApiRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pollClientMock = new Mock<IMaxHttpClient>();
        pollClientMock
            .Setup(x => x.SendAsyncRaw(
                It.IsAny<MaxApiRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns<MaxApiRequest, CancellationToken>((req, _) =>
            {
                updatesRequestSeen.TrySetResult(req);
                return Task.FromResult("{\"updates\":[],\"marker\":1}");
            });

        var subscriptionsApiMock = new Mock<ISubscriptionsApi>();
        var botApiMock = new Mock<IMaxBotApi>();
        var handlerMock = new Mock<IUpdateHandler>();

        var poller = new UpdatePoller(
            botApiMock.Object,
            subscriptionsApiMock.Object,
            options,
            pollClientMock.Object);

        await poller.StartAsync(handlerMock.Object, CancellationToken.None);
        var request = await updatesRequestSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None);

        request.Method.Should().Be(HttpMethod.Get);
        request.Endpoint.Should().Be("/updates");
        request.Headers.Should().NotBeNull();
        request.Headers.Should().ContainKey("Authorization");
        request.Headers!["Authorization"].Should().Be("test-token-123");
    }
}
