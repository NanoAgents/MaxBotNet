using System.Net;
using System.Net.Http;
using FluentAssertions;
using Max.Bot.Api;
using Max.Bot.Configuration;
using Max.Bot.Exceptions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Max.Bot.Types.Requests;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Api;

public class SubscriptionsApiTests
{
    private readonly Mock<IMaxHttpClient> _mockHttpClient;
    private readonly MaxBotOptions _options;

    public SubscriptionsApiTests()
    {
        _mockHttpClient = new Mock<IMaxHttpClient>();
        _options = new MaxBotOptions
        {
            Token = "test-token-123",
            BaseUrl = "https://api.max.ru/bot"
        };
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldReturnSubscriptions_WhenRequestSucceeds()
    {
        // Arrange
        var expectedSubscriptions = new[]
        {
            new Subscription { Url = "https://example.com/webhook1" },
            new Subscription { Url = "https://example.com/webhook2" }
        };

        var subscriptionsResponse = new SubscriptionsResponse
        {
            Subscriptions = expectedSubscriptions
        };

        var responseJson = MaxJsonSerializer.Serialize(subscriptionsResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/subscriptions"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var result = await subscriptionsApi.GetSubscriptionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Url.Should().Be("https://example.com/webhook1");
        result[1].Url.Should().Be("https://example.com/webhook2");
    }

    [Fact]
    public async Task GetSubscriptionsAsync_ShouldThrowMaxApiException_WhenResponseIsNull()
    {
        // Arrange
        _mockHttpClient
            .Setup(x => x.SendAsync<SubscriptionsResponse>(
                It.IsAny<MaxApiRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubscriptionsResponse)null!);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await subscriptionsApi.GetSubscriptionsAsync();

        // Assert
        await act.Should().ThrowAsync<MaxApiException>();
    }

    [Fact]
    public async Task SetWebhookAsync_ShouldReturnResponse_WhenRequestSucceeds()
    {
        // Arrange
        var request = new SetWebhookRequest
        {
            Url = "https://example.com/webhook",
            UpdateTypes = new List<UpdateType> { UpdateType.MessageCreated },
            Secret = "my-secret-123"
        };

        var response = new Response
        {
            Success = true,
            Message = "Webhook set successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Endpoint == "/subscriptions" &&
                    req.Body != null &&
                    req.Body.GetType() == typeof(SetWebhookRequest)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var result = await subscriptionsApi.SetWebhookAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Webhook set successfully");
    }

    [Fact]
    public async Task SetWebhookAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await subscriptionsApi.SetWebhookAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task SetWebhookAsync_ShouldSendJsonBody_WithOptionalFields()
    {
        // Arrange
        var request = new SetWebhookRequest
        {
            Url = "https://example.com/webhook",
            UpdateTypes = null,
            Secret = null
        };

        var response = new Response { Success = true };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Post &&
                    req.Body != null &&
                    req.Body.GetType() == typeof(SetWebhookRequest)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        await subscriptionsApi.SetWebhookAsync(request);

        // Assert
        _mockHttpClient.Verify(x => x.SendAsync<Response>(
            It.IsAny<MaxApiRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteWebhookAsync_ShouldReturnResponse_WhenRequestSucceeds()
    {
        // Arrange
        var response = new Response
        {
            Success = true,
            Message = "Webhook deleted successfully"
        };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.Endpoint == "/subscriptions" &&
                    req.Body == null &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("url") &&
                    req.QueryParameters["url"] == "https://example.com/webhook"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var result = await subscriptionsApi.DeleteWebhookAsync("https://example.com/webhook");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Webhook deleted successfully");
    }

    [Fact]
    public async Task DeleteWebhookAsync_ShouldSendUrlInQuery_WhenRequestProvided()
    {
        // Arrange
        var response = new Response { Success = true };

        _mockHttpClient
            .Setup(x => x.SendAsync<Response>(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.Body == null &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("url") &&
                    req.QueryParameters["url"] == "https://example.com/webhook"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        await subscriptionsApi.DeleteWebhookAsync("https://example.com/webhook");

        // Assert
        _mockHttpClient.Verify(x => x.SendAsync<Response>(
            It.Is<MaxApiRequest>(req =>
                req.Method == HttpMethod.Delete &&
                req.Body == null &&
                req.QueryParameters != null &&
                req.QueryParameters.ContainsKey("url") &&
                req.QueryParameters["url"] == "https://example.com/webhook"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldReturnGetUpdatesResponse_WhenRequestSucceeds()
    {
        // Arrange
        var request = new GetUpdatesRequest
        {
            Limit = 50,
            Timeout = 30,
            Marker = 12345,
            Types = new List<UpdateType> { UpdateType.MessageCreated, UpdateType.MessageCallback }
        };

        var expectedUpdates = new[]
        {
            new Update
            {
                UpdateId = 1,
                UpdateTypeRaw = "message_created",
                Message = new Message { Timestamp = 1609459200000, Body = new MessageBody { Mid = "mid.100", Text = "Test" } }
            }
        };

        var expectedResponse = new GetUpdatesResponse
        {
            Updates = expectedUpdates,
            Marker = 12346
        };

        var wrappedResponse = new Response<GetUpdatesResponse>
        {
            Ok = true,
            Result = expectedResponse
        };

        var responseJson = MaxJsonSerializer.Serialize(wrappedResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/updates" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("limit") &&
                    req.QueryParameters["limit"] == "50" &&
                    req.QueryParameters.ContainsKey("timeout") &&
                    req.QueryParameters["timeout"] == "30" &&
                    req.QueryParameters.ContainsKey("marker") &&
                    req.QueryParameters["marker"] == "12345" &&
                    req.QueryParameters.ContainsKey("types") &&
                    req.QueryParameters["types"] == "message_created,message_callback"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var result = await subscriptionsApi.GetUpdatesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Updates.Should().HaveCount(1);
        result.Updates[0].UpdateId.Should().Be(1);
        result.Marker.Should().Be(12346);
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldPreferDirectGetUpdatesResponse()
    {
        // Arrange
        var directJson = """
        {
          "updates": [],
          "marker": 123,
          "ok": true,
          "result": {
            "updates": [],
            "marker": 999
          }
        }
        """;

        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.IsAny<MaxApiRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(directJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var result = await subscriptionsApi.GetUpdatesAsync(new GetUpdatesRequest());

        // Assert
        result.Marker.Should().Be(123);
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        var act = async () => await subscriptionsApi.GetUpdatesAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldNotIncludeOptionalParameters_WhenNotSpecified()
    {
        // Arrange
        var request = new GetUpdatesRequest
        {
            Limit = null,
            Timeout = null,
            Marker = null,
            Types = null
        };

        var expectedResponse = new GetUpdatesResponse
        {
            Updates = Array.Empty<Update>(),
            Marker = null
        };

        var wrappedResponse = new Response<GetUpdatesResponse>
        {
            Ok = true,
            Result = expectedResponse
        };

        var responseJson = MaxJsonSerializer.Serialize(wrappedResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/updates" &&
                    (req.QueryParameters == null || req.QueryParameters.Count == 0)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        await subscriptionsApi.GetUpdatesAsync(request);

        // Assert
        _mockHttpClient.Verify(x => x.SendAsyncRaw(
            It.IsAny<MaxApiRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldSendSingleTypeInQueryParams()
    {
        // Arrange
        var request = new GetUpdatesRequest
        {
            Limit = 10,
            Types = new List<UpdateType> { UpdateType.MessageCreated }
        };

        var expectedResponse = new GetUpdatesResponse
        {
            Updates = Array.Empty<Update>(),
            Marker = null
        };

        var wrappedResponse = new Response<GetUpdatesResponse>
        {
            Ok = true,
            Result = expectedResponse
        };

        var responseJson = MaxJsonSerializer.Serialize(wrappedResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/updates" &&
                    req.QueryParameters != null &&
                    req.QueryParameters.ContainsKey("types") &&
                    req.QueryParameters["types"] == "message_created"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        await subscriptionsApi.GetUpdatesAsync(request);

        // Assert
        _mockHttpClient.Verify(x => x.SendAsyncRaw(
            It.IsAny<MaxApiRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUpdatesAsync_ShouldSendEmptyTypesList_WhenEmpty()
    {
        // Arrange
        var request = new GetUpdatesRequest
        {
            Limit = 10,
            Types = new List<UpdateType>()
        };

        var expectedResponse = new GetUpdatesResponse
        {
            Updates = Array.Empty<Update>(),
            Marker = null
        };

        var wrappedResponse = new Response<GetUpdatesResponse>
        {
            Ok = true,
            Result = expectedResponse
        };

        var responseJson = MaxJsonSerializer.Serialize(wrappedResponse);
        _mockHttpClient
            .Setup(x => x.SendAsyncRaw(
                It.Is<MaxApiRequest>(req =>
                    req.Method == HttpMethod.Get &&
                    req.Endpoint == "/updates" &&
                    (req.QueryParameters == null || !req.QueryParameters.ContainsKey("types"))),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseJson);

        var subscriptionsApi = new SubscriptionsApi(_mockHttpClient.Object, _options);

        // Act
        await subscriptionsApi.GetUpdatesAsync(request);

        // Assert
        _mockHttpClient.Verify(x => x.SendAsyncRaw(
            It.IsAny<MaxApiRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

