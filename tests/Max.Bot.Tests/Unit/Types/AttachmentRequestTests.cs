using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using Max.Bot.Types.Requests;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

/// <summary>
/// Unit tests for <see cref="AttachmentRequest"/>.
/// </summary>
public class AttachmentRequestTests
{
    [Fact]
    public void Type_ShouldBeRequired()
    {
        // Arrange
        var request = new AttachmentRequest
        {
            Type = null!,
            Payload = new { token = "test" }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().Contain(v => v.MemberNames.Contains("Type") && v.ErrorMessage != null && v.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Payload_ShouldBeOptional_ForLocationAttachments()
    {
        // Arrange — MAX API location attachments carry latitude/longitude instead of a payload
        var request = AttachmentRequest.Location(55.75, 37.62);

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        request.Payload.Should().BeNull();
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void ShouldSerializeCorrectly()
    {
        // Arrange
        var request = new AttachmentRequest
        {
            Type = "video",
            Payload = new { token = "test-token-123" }
        };

        // Act
        var json = JsonSerializer.Serialize(request);

        // Assert
        json.Should().Contain("\"type\":\"video\"");
        json.Should().Contain("\"payload\"");
    }

    [Theory]
    [InlineData("image")]
    [InlineData("video")]
    [InlineData("audio")]
    [InlineData("file")]
    public void Type_ShouldAcceptValidTypes(string type)
    {
        // Arrange
        var request = new AttachmentRequest
        {
            Type = type,
            Payload = new { token = "test" }
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().NotContain(v => v.MemberNames.Contains("Type"));
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}

