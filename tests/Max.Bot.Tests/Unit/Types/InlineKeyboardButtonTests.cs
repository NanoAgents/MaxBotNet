using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class InlineKeyboardButtonTests
{
    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithNewFormat()
    {
        // Arrange
        var json = """{"type":"callback","text":"Button Text","payload":"callback123"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Type.Should().Be(ButtonType.Callback);
        button.Text.Should().Be("Button Text");
        button.Payload.Should().Be("callback123");
        button.CallbackData.Should().Be("callback123");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithLegacyCallbackData()
    {
        // Arrange - legacy format for backward compatibility
        var json = """{"text":"Button Text","callback_data":"callback123"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Type.Should().Be(ButtonType.Callback);
        button.Text.Should().Be("Button Text");
        button.Payload.Should().Be("callback123");
        button.CallbackData.Should().Be("callback123");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithUrl()
    {
        // Arrange
        var json = """{"type":"link","text":"Open URL","url":"https://example.com"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Type.Should().Be(ButtonType.Link);
        button.Text.Should().Be("Open URL");
        button.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_ToJson_WithCallbackType()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Callback,
            Text = "Button Text",
            Payload = "callback123"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"type\":\"callback\"");
        json.Should().Contain("\"text\":\"Button Text\"");
        json.Should().Contain("\"payload\":\"callback123\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_ToJson_UsingCallbackData()
    {
        // Arrange - using CallbackData property for backward compatibility
        var button = new InlineKeyboardButton
        {
            Text = "Button Text",
            CallbackData = "callback123"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"type\":\"callback\"");
        json.Should().Contain("\"text\":\"Button Text\"");
        json.Should().Contain("\"payload\":\"callback123\"");
        button.Type.Should().Be(ButtonType.Callback);
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_WithUrl()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Link,
            Text = "Open URL",
            Url = "https://example.com"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"type\":\"link\"");
        json.Should().Contain("\"text\":\"Open URL\"");
        json.Should().Contain("\"url\":\"https://example.com\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_OpenAppAsLink()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = (ButtonType)5,
            Text = "Open App",
            Url = "https://example.com/app"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"type\":\"link\"");
        json.Should().Contain("\"url\":\"https://example.com/app\"");
        json.Should().NotContain("\"type\":\"open_app\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldAutoSetType_WhenUrlIsSet()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Text = "Open URL"
        };

        // Act
        button.Url = "https://example.com";

        // Assert
        button.Type.Should().Be(ButtonType.Link);
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithIntent()
    {
        // Arrange
        var json = """{"type":"callback","text":"Confirm","payload":"confirm","intent":"positive"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Type.Should().Be(ButtonType.Callback);
        button.Text.Should().Be("Confirm");
        button.Payload.Should().Be("confirm");
        button.Intent.Should().Be(ButtonIntent.Positive);
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithNegativeIntent()
    {
        // Arrange
        var json = """{"type":"callback","text":"Cancel","payload":"cancel","intent":"negative"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Intent.Should().Be(ButtonIntent.Negative);
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithDefaultIntent()
    {
        // Arrange
        var json = """{"type":"callback","text":"Action","payload":"action","intent":"default"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Intent.Should().Be(ButtonIntent.Default);
    }

    [Fact]
    public void InlineKeyboardButton_ShouldDeserialize_FromJson_WithoutIntent()
    {
        // Arrange
        var json = """{"type":"callback","text":"Button","payload":"data"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Intent.Should().BeNull();
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_ToJson_WithIntent()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Callback,
            Text = "Confirm",
            Payload = "confirm",
            Intent = ButtonIntent.Positive
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"type\":\"callback\"");
        json.Should().Contain("\"text\":\"Confirm\"");
        json.Should().Contain("\"payload\":\"confirm\"");
        json.Should().Contain("\"intent\":\"positive\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_ToJson_WithNegativeIntent()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Callback,
            Text = "Cancel",
            Payload = "cancel",
            Intent = ButtonIntent.Negative
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().Contain("\"intent\":\"negative\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldSerialize_ToJson_WithoutIntent_WhenNull()
    {
        // Arrange
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Callback,
            Text = "Button",
            Payload = "data",
            Intent = null
        };

        // Act
        var json = MaxJsonSerializer.Serialize(button);

        // Assert
        json.Should().NotContain("\"intent\"");
    }

    [Fact]
    public void InlineKeyboardButton_ShouldIgnoreIntent_WhenValueIsNumeric()
    {
        // Arrange
        var json = """{"type":"callback","text":"Action","payload":"action","intent":"1"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Intent.Should().BeNull();
    }

    [Fact]
    public void InlineKeyboardButton_ShouldNotTreatNumericTypeAsEnumValue()
    {
        // Arrange
        var json = """{"type":"1","text":"NumericType"}""";

        // Act
        var button = MaxJsonSerializer.Deserialize<InlineKeyboardButton>(json);

        // Assert
        button.Should().NotBeNull();
        button.Type.Should().Be(ButtonType.Callback);
    }
}

