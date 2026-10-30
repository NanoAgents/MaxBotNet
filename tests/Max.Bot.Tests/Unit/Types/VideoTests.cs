using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class VideoTests
{
    [Fact]
    public void Video_ShouldDeserialize_FromJson()
    {
        // Arrange
        var json = """{"id":123,"file_id":"video123","width":1280,"height":720,"duration":120,"file_size":1048576,"mime_type":"video/mp4","url":"https://example.com/video.mp4"}""";

        // Act
        var video = MaxJsonSerializer.Deserialize<Video>(json);

        // Assert
        video.Should().NotBeNull();
        video.Id.Should().Be(123);
        video.FileId.Should().Be("video123");
        video.Width.Should().Be(1280);
        video.Height.Should().Be(720);
        video.Duration.Should().Be(120);
        video.FileSize.Should().Be(1048576);
        video.MimeType.Should().Be("video/mp4");
        video.Url.Should().Be("https://example.com/video.mp4");
    }

    [Fact]
    public void Video_ShouldDeserialize_WithNullableFields()
    {
        // Arrange
        var json = """{"id":123,"file_id":"video123"}""";

        // Act
        var video = MaxJsonSerializer.Deserialize<Video>(json);

        // Assert
        video.Should().NotBeNull();
        video.Id.Should().Be(123);
        video.FileId.Should().Be("video123");
        video.Width.Should().Be(0);
        video.Height.Should().Be(0);
        video.Duration.Should().Be(0);
        video.FileSize.Should().BeNull();
        video.MimeType.Should().BeNull();
        video.Url.Should().BeNull();
    }

    [Fact]
    public void Video_ShouldSerialize_ToJson()
    {
        // Arrange
        var video = new Video
        {
            Id = 123,
            FileId = "video123",
            Width = 1280,
            Height = 720,
            Duration = 120,
            FileSize = 1048576,
            MimeType = "video/mp4",
            Url = "https://example.com/video.mp4"
        };

        // Act
        var json = MaxJsonSerializer.Serialize(video);

        // Assert
        json.Should().Contain("\"id\":123");
        json.Should().Contain("\"file_id\":\"video123\"");
        json.Should().Contain("\"width\":1280");
        json.Should().Contain("\"height\":720");
        json.Should().Contain("\"duration\":120");
        json.Should().Contain("\"file_size\":1048576");
        json.Should().Contain("\"mime_type\":\"video/mp4\"");
        json.Should().Contain("\"url\":\"https://example.com/video.mp4\"");
    }
}

