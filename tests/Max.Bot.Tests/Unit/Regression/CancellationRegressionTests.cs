// 📁 CancellationRegressionTests.cs - Regression tests for cancellation handling (v0.8.1-alpha)
// 🎯 Core function: Pins that stopping the bot is not reported as a network or handler error.
// 🔗 Key dependencies: UpdatePoller, MaxHttpClient, UpdateHandlerExecutor.
// 💡 Usage: Each test documents the failure scenario it guards against.

using System.Net.Http;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Polling;
using Max.Bot.Types;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Regression;

public class CancellationRegressionTests
{
    private const string OneUpdateJson = """{"updates":[{"update_type":"message_created","timestamp":1,"message":{"recipient":{"chat_id":1,"chat_type":"dialog"},"timestamp":1,"body":{"mid":"m","seq":1,"text":"hi"}}}],"marker":2}""";

    /// <summary>
    /// Reported: stopping the bot logged "Network error while polling updates" with MaxNetworkException wrapping
    /// TaskCanceledException, because FetchUpdatesAsync wrapped the cancellation of the long-polling request.
    /// </summary>
    [Fact]
    public async Task StopAsync_DuringLongPollingRequest_ShouldNotLogNetworkError()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pollClient = new Mock<IMaxHttpClient>();
        pollClient
            .Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MaxApiRequest, CancellationToken>(async (_, ct) =>
            {
                requestStarted.TrySetResult();
                await Task.Delay(Timeout.Infinite, ct);
                return "{}";
            });

        var logger = new RecordingLogger<UpdatePoller>();
        var poller = CreatePoller(pollClient.Object, logger);

        await poller.StartAsync(Mock.Of<IUpdateHandler>(), CancellationToken.None);
        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning);
        logger.Entries.Should().Contain(e => e.Message.Contains("UpdatePoller stopped."));
    }

    [Fact]
    public async Task StopAsync_DuringErrorBackoff_ShouldExitWithoutUnobservedCancellation()
    {
        var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pollClient = new Mock<IMaxHttpClient>();
        pollClient
            .Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MaxApiRequest, CancellationToken>((_, _) =>
            {
                failed.TrySetResult();
                throw new HttpRequestException("network down");
            });

        var options = CreateOptions();
        options.Polling.ErrorBackoffBase = TimeSpan.FromMinutes(1);
        options.Polling.ErrorBackoffMax = TimeSpan.FromMinutes(1);
        var logger = new RecordingLogger<UpdatePoller>();
        var poller = CreatePoller(pollClient.Object, logger, options);

        await poller.StartAsync(Mock.Of<IUpdateHandler>(), CancellationToken.None);
        await failed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        // The real network error is still reported once; stopping during the backoff adds nothing.
        logger.Entries.Count(e => e.Level == LogLevel.Warning).Should().Be(1);
        logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task StopAsync_WhileHandlerObservesCancellation_ShouldNotLogHandlerError()
    {
        var handlerStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var pollClient = new Mock<IMaxHttpClient>();
        pollClient
            .Setup(x => x.SendAsyncRaw(It.IsAny<MaxApiRequest>(), It.IsAny<CancellationToken>()))
            .Returns<MaxApiRequest, CancellationToken>(async (_, ct) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    return OneUpdateJson;
                }

                await Task.Delay(Timeout.Infinite, ct);
                return "{}";
            });

        var handler = new DelegatingUpdateHandler(onMessage: async (_, ct) =>
        {
            handlerStarted.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
        });

        var logger = new RecordingLogger<UpdatePoller>();
        var poller = CreatePoller(pollClient.Object, logger);

        await poller.StartAsync(handler, CancellationToken.None);
        await handlerStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await poller.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning);
        calls.Should().Be(1, "the batch was interrupted, so the marker is not advanced and no further request is made");
    }

    [Fact]
    public async Task MaxHttpClient_CancelledByCaller_ShouldNotRetryNorLogFailure()
    {
        using var cts = new CancellationTokenSource();
        var attempts = 0;
        var handler = new CallbackHandler(async ct =>
        {
            Interlocked.Increment(ref attempts);
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        });

        var logger = new RecordingLogger<MaxHttpClient>();
        using var client = new MaxHttpClient(
            new HttpClient(handler),
            new MaxBotClientOptions { BaseUrl = "https://api.max.ru/bot", RetryCount = 3, RetryBaseDelay = TimeSpan.FromMilliseconds(1) },
            logger);

        var act = () => client.SendAsyncRaw(new MaxApiRequest { Method = HttpMethod.Get, Endpoint = "/me" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1);
        logger.Entries.Should().NotContain(e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task MaxHttpClient_HttpClientTimeout_ShouldStillBeRetried()
    {
        var attempts = 0;
        var handler = new CallbackHandler(_ =>
        {
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw new TaskCanceledException("timeout");
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}") });
        });

        using var client = new MaxHttpClient(
            new HttpClient(handler),
            new MaxBotClientOptions { BaseUrl = "https://api.max.ru/bot", RetryCount = 2, RetryBaseDelay = TimeSpan.FromMilliseconds(1) });

        var body = await client.SendAsyncRaw(new MaxApiRequest { Method = HttpMethod.Get, Endpoint = "/me" }, CancellationToken.None);

        body.Should().Be("{}");
        attempts.Should().Be(2);
    }

    [Fact]
    public async Task UpdateHandlerExecutor_CancelledByCaller_ShouldPropagateCancellationWithoutErrorLog()
    {
        using var cts = new CancellationTokenSource();
        var handler = new DelegatingUpdateHandler(onMessage: async (_, ct) =>
        {
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ct);
        });
        var update = Max.Bot.Networking.MaxJsonSerializer.Deserialize<GetUpdatesResponse>(OneUpdateJson).Updates[0];
        var logger = new RecordingLogger<UpdatePoller>();

        var act = () => UpdateHandlerExecutor.ExecuteAsync(update, handler, CreateOptions(), Mock.Of<IMaxBotApi>(), logger, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.Entries.Should().BeEmpty();
    }

    private static MaxBotOptions CreateOptions() => new()
    {
        Token = "test-token",
        BaseUrl = "https://api.max.ru/bot"
    };

    private static UpdatePoller CreatePoller(IMaxHttpClient pollClient, ILogger<UpdatePoller> logger, MaxBotOptions? options = null) =>
        new(Mock.Of<IMaxBotApi>(), Mock.Of<ISubscriptionsApi>(), options ?? CreateOptions(), pollClient, logger);

    private sealed class CallbackHandler(Func<CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(cancellationToken);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = new();

        public IReadOnlyList<(LogLevel Level, string Message)> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.ToArray();
                }
            }
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (_entries)
            {
                _entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }
}
