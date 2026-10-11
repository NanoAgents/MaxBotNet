// 📁 WebhookControllerTests.cs - Tests for webhook controller behavior
// 🎯 Core function: Ensures webhook auth, size limits and pipeline dispatch work correctly.
// 🔗 Key dependencies: WebhookController, IUpdatePipeline, ASP.NET Core MVC.
// 💡 Usage: Verifies webhook ingestion for message_created updates.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Max.Bot.Configuration;
using Max.Bot.Polling;
using Max.Bot.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Max.Bot.Tests.Unit.Polling;

public class WebhookControllerTests
{
    private const string UpdateJson = """{"update_type":"message_created","timestamp":1,"message":{"sender":{"user_id":1,"username":"tester"},"recipient":{"chat_id":1,"chat_type":"dialog"},"body":{"mid":"mid.1","text":"hi"}}}""";

    [Fact]
    public async Task PostAsync_ShouldReturnUnauthorized_WhenSignatureMissing()
    {
        // Arrange
        var (controller, pipelineMock, _) = CreateController(secret: "secret");
        controller.ControllerContext = new ControllerContext { HttpContext = CreateContext(UpdateJson) };

        // Act
        var result = await controller.PostAsync(CancellationToken.None);

        // Assert
        result.As<StatusCodeResult>().StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        pipelineMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostAsync_ShouldReturnPayloadTooLarge_WhenBodyExceedsLimit()
    {
        // Arrange
        var (controller, _, _) = CreateController(secret: null);
        var context = CreateContext(UpdateJson);
        context.Request.ContentLength = (controller.WebhookOptions.MaxBodySizeKilobytes + 1) * 1024L;
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await controller.PostAsync(CancellationToken.None);

        // Assert
        result.As<StatusCodeResult>().StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnPayloadTooLarge_WhenChunkedBodyExceedsLimit()
    {
        // Arrange — no Content-Length (chunked transfer), the body itself is over the limit
        var (controller, pipelineMock, _) = CreateController(secret: null);
        var context = CreateContext("{\"update_type\":\"" + new string('x', 4096) + "\"}", setContentLength: false);
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await controller.PostAsync(CancellationToken.None);

        // Assert
        result.As<StatusCodeResult>().StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        pipelineMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenBodyIsInvalidJson()
    {
        // Arrange
        var (controller, pipelineMock, _) = CreateController(secret: null);
        controller.ControllerContext = new ControllerContext { HttpContext = CreateContext("not json") };

        // Act
        var result = await controller.PostAsync(CancellationToken.None);

        // Assert
        result.As<StatusCodeResult>().StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        pipelineMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostAsync_ShouldProcess_WhenSignatureValid()
    {
        // Arrange
        var secret = "secret-token";
        var (controller, pipelineMock, handlerMock) = CreateController(secret);
        var context = CreateContext(UpdateJson);
        context.Request.Headers[controller.WebhookOptions.SignatureHeaderName] = secret;
        controller.ControllerContext = new ControllerContext { HttpContext = context };

        // Act
        var result = await controller.PostAsync(CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        pipelineMock.Verify(p => p.ProcessWebhookAsync(
            It.Is<Update>(u => u.Message!.Mid == "mid.1"),
            handlerMock.Object,
            context.RequestServices,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static (TestableWebhookController Controller, Mock<IUpdatePipeline> Pipeline, Mock<IUpdateHandler> Handler) CreateController(string? secret)
    {
        var pipeline = new Mock<IUpdatePipeline>();
        pipeline.Setup(p => p.ProcessWebhookAsync(It.IsAny<Update>(), It.IsAny<IUpdateHandler>(), It.IsAny<IServiceProvider?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new Mock<IUpdateHandler>();

        var options = new MaxBotOptions
        {
            Token = "token",
            BaseUrl = "https://api.max.ru/bot"
        };
        options.Webhook.SecretToken = secret;
        options.Webhook.SignatureHeaderName = "X-Test-Signature";
        options.Webhook.MaxBodySizeKilobytes = 1;

        var controller = new TestableWebhookController(pipeline.Object, handler.Object, options, Mock.Of<ILogger<WebhookController>>());
        return (controller, pipeline, handler);
    }

    private static DefaultHttpContext CreateContext(string body, bool setContentLength = true)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(bytes);
        if (setContentLength)
        {
            context.Request.ContentLength = bytes.Length;
        }

        return context;
    }

    private sealed class TestableWebhookController : WebhookController
    {
        public TestableWebhookController(IUpdatePipeline pipeline, IUpdateHandler handler, MaxBotOptions options, ILogger<WebhookController>? logger)
            : base(pipeline, handler, options, logger)
        {
            WebhookOptions = options.Webhook;
        }

        public MaxWebhookOptions WebhookOptions { get; }
    }
}
