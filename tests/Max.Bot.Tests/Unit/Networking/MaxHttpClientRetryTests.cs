using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Xunit;

namespace Max.Bot.Tests.Unit.Networking;

public class MaxHttpClientRetryTests
{
    private readonly MaxBotClientOptions _defaultOptions;

    public MaxHttpClientRetryTests()
    {
        _defaultOptions = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = TimeSpan.FromSeconds(30),
            RetryCount = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(100),
            MaxRetryDelay = TimeSpan.FromSeconds(10)
        };
    }

    [Fact]
    public async Task SendAsync_ShouldRetry_OnNetworkException()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new HttpRequestException("Network error");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var result = await client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task SendAsync_ShouldRetry_OnRateLimitException()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("Too Many Requests", Encoding.UTF8, "text/plain"),
                        Headers = { { "Retry-After", "1" } }
                    };
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var result = await client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_ShouldRetry_On5xxError()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                if (attemptCount < 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var result = await client.SendAsync<TestResponse>(request);

        // Assert
        result.Should().NotBeNull();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_ShouldNotRetry_OnUnauthorizedException()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var act = async () => await client.SendAsync<object>(request);

        // Assert
        await act.Should().ThrowAsync<MaxUnauthorizedException>();
        attemptCount.Should().Be(1); // No retries
    }

    [Fact]
    public async Task SendAsync_ShouldNotRetry_OnBadRequest()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Bad Request", Encoding.UTF8, "text/plain")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var act = async () => await client.SendAsync<object>(request);

        // Assert
        await act.Should().ThrowAsync<MaxApiException>();
        attemptCount.Should().Be(1); // No retries
    }

    [Fact]
    public async Task SendAsync_ShouldStopRetrying_AfterMaxAttempts()
    {
        // Arrange
        var attemptCount = 0;
        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
                };
            }
        };
        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        var act = async () => await client.SendAsync<object>(request);

        // Assert
        await act.Should().ThrowAsync<MaxNetworkException>();
        // Should try 1 initial attempt + 3 retries = 4 total attempts
        attemptCount.Should().Be(_defaultOptions.RetryCount + 1);
    }

    [Fact]
    public async Task SendAsync_ShouldUseExponentialBackoff()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<TimeSpan>();
        var stopwatch = Stopwatch.StartNew();
        var previousTime = TimeSpan.Zero;

        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                var currentTime = stopwatch.Elapsed;
                if (attemptCount > 1)
                {
                    delays.Add(currentTime - previousTime);
                }

                previousTime = currentTime;

                if (attemptCount < 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Internal Server Error", Encoding.UTF8, "text/plain")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                };
            }
        };

        var options = new MaxBotClientOptions
        {
            BaseUrl = "https://api.max.ru/bot",
            Timeout = TimeSpan.FromSeconds(30),
            RetryCount = 3,
            RetryBaseDelay = TimeSpan.FromMilliseconds(50),
            MaxRetryDelay = TimeSpan.FromSeconds(10)
        };

        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, options);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        await client.SendAsync<TestResponse>(request);
        stopwatch.Stop();

        // Assert
        attemptCount.Should().Be(3);
        delays.Should().HaveCount(2);

        delays[0].TotalMilliseconds.Should().BeGreaterThan(30);
        delays[1].TotalMilliseconds.Should().BeGreaterThan(60);
    }

    [Fact]
    public async Task SendAsync_ShouldUseRetryAfter_ForRateLimit()
    {
        // Arrange
        var attemptCount = 0;
        var delays = new List<TimeSpan>();
        var stopwatch = Stopwatch.StartNew();
        var previousTime = TimeSpan.Zero;

        var handler = new TestHttpMessageHandler
        {
            ResponseFactory = (req) =>
            {
                attemptCount++;
                var currentTime = stopwatch.Elapsed;
                if (attemptCount > 1)
                {
                    delays.Add(currentTime - previousTime);
                }

                previousTime = currentTime;

                if (attemptCount < 2)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("Too Many Requests", Encoding.UTF8, "text/plain")
                    };
                    response.Headers.Add("Retry-After", "1"); // 1 second
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\":\"success\"}", Encoding.UTF8, "application/json")
                };
            }
        };

        var httpClient = new HttpClient(handler);
        var client = new MaxHttpClient(httpClient, _defaultOptions);
        var request = new MaxApiRequest
        {
            Method = HttpMethod.Get,
            Endpoint = "test"
        };

        // Act
        await client.SendAsync<TestResponse>(request);
        stopwatch.Stop();

        // Assert
        attemptCount.Should().Be(2);
        delays.Should().HaveCount(1);
        // Delay should be approximately Retry-After (1 second) with some tolerance
        delays[0].TotalMilliseconds.Should().BeGreaterThan(900).And.BeLessThan(1200);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage? Response { get; set; }
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }
        public List<HttpRequestMessage> Requests { get; } = new();
        public Action<HttpRequestMessage>? OnRequest { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            OnRequest?.Invoke(request);

            if (ResponseFactory != null)
            {
                var response = ResponseFactory(request);
                return Task.FromResult(response);
            }

            if (Response != null)
            {
                return Task.FromResult(Response);
            }

            // Default: simulate network error
            throw new HttpRequestException("Network error");
        }
    }

    private class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}

