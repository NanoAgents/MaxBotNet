using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class UserTests
{
    [Fact]
    public void Deserialize_ShouldDeserializeUser()
    {
        // Arrange
        var json = """{"user_id":123,"username":"testuser","first_name":"Test","last_name":"User","is_bot":false,"name":"Test User","description":"Bot description","avatar_url":"https://example.com/avatar.png","full_avatar_url":"https://example.com/avatar_full.png","commands":[{"name":"start","description":"Start bot"}]}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<User>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(123);
        result.Username.Should().Be("testuser");
        result.FirstName.Should().Be("Test");
        result.LastName.Should().Be("User");
        result.IsBot.Should().BeFalse();
        result.Name.Should().Be("Test User");
        result.Description.Should().Be("Bot description");
        result.AvatarUrl.Should().Be("https://example.com/avatar.png");
        result.FullAvatarUrl.Should().Be("https://example.com/avatar_full.png");
        result.Commands.Should().ContainSingle();
        result.Commands![0].Name.Should().Be("start");
    }

    [Fact]
    public void Deserialize_ShouldDeserializeUserWithNullFields()
    {
        // Arrange
        var json = """{"user_id":123,"is_bot":true}""";

        // Act
        var result = MaxJsonSerializer.Deserialize<User>(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(123);
        result.Username.Should().BeNull();
        result.FirstName.Should().BeEmpty();
        result.LastName.Should().BeNull();
        result.IsBot.Should().BeTrue();
    }

    [Fact]
    public void Serialize_ShouldSerializeUser()
    {
        // Arrange
        var user = new User
        {
            Id = 123,
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            IsBot = false,
            Name = "Test User",
            Description = "Bot description",
            AvatarUrl = "https://example.com/avatar.png",
            FullAvatarUrl = "https://example.com/avatar_full.png",
            Commands = new[] { new BotCommand("start", "Start bot") }
        };

        // Act
        var json = MaxJsonSerializer.Serialize(user);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"user_id\":123");
        json.Should().Contain("\"username\":\"testuser\"");
        json.Should().Contain("\"first_name\":\"Test\"");
        json.Should().Contain("\"last_name\":\"User\"");
        json.Should().Contain("\"is_bot\":false");
        json.Should().Contain("\"name\":\"Test User\"");
        json.Should().Contain("\"description\":\"Bot description\"");
        json.Should().Contain("\"avatar_url\":\"https://example.com/avatar.png\"");
        json.Should().Contain("\"full_avatar_url\":\"https://example.com/avatar_full.png\"");
        json.Should().Contain("\"commands\"");
    }

    [Fact]
    public void Serialize_ShouldNotIncludeNullFields()
    {
        // Arrange
        var user = new User
        {
            Id = 123,
            IsBot = true
        };

        // Act
        var json = MaxJsonSerializer.Serialize(user);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"user_id\":123");
        json.Should().Contain("\"is_bot\":true");
        json.Should().NotContain("\"username\"");
        json.Should().NotContain("\"firstName\"");
        json.Should().NotContain("\"lastName\"");
    }
}

