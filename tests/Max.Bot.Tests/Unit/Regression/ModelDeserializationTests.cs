// 📁 ModelDeserializationTests.cs - Regression tests for typed models in v0.8.0-alpha
// 🎯 Core function: Pins that enums and models deserialize with any JsonSerializerOptions and expose typed values.
// 🔗 Key dependencies: Max.Bot.Types models, enums, MaxJsonSerializer.
// 💡 Usage: Each test documents the failure scenario it guards against.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Enums;
using Xunit;

namespace Max.Bot.Tests.Unit.Regression;

public class ModelDeserializationTests
{
    private static readonly JsonSerializerOptions DefaultOptions = new();

    private static readonly Assembly LibraryAssembly = typeof(Message).Assembly;

    private const string MessageCreatedJson = """
        {
          "update_type": "message_created",
          "timestamp": 1730300000000,
          "user_locale": "ru",
          "message": {
            "sender": { "user_id": 1, "first_name": "Ann", "last_name": null, "username": null, "is_bot": false, "last_activity_time": 1730300000000 },
            "recipient": { "chat_id": -42, "chat_type": "chat", "user_id": null },
            "timestamp": 1730300000000,
            "link": {
              "type": "reply",
              "sender": { "user_id": 2, "first_name": "Bob", "is_bot": false, "last_activity_time": 1 },
              "message": { "mid": "mid.original", "seq": 7, "text": "original", "attachments": null }
            },
            "body": {
              "mid": "mid.reply",
              "seq": 8,
              "text": "hello @bob",
              "attachments": [
                {
                  "type": "inline_keyboard",
                  "callback_id": "kb-1",
                  "payload": {
                    "buttons": [
                      [ { "type": "callback", "text": "Yes", "payload": "yes", "intent": "positive" } ],
                      [ { "type": "chat", "text": "Discuss", "chat_title": "Topic", "start_payload": "topic-1", "uuid": 123 } ]
                    ]
                  }
                },
                { "type": "data", "data": "reply-keyboard-payload" }
              ],
              "markup": [ { "type": "user_mention", "from": 6, "length": 4, "user_id": 2 } ]
            },
            "stat": { "views": 5 }
          }
        }
        """;

    public static TheoryData<Type> LibraryEnums()
    {
        var data = new TheoryData<Type>();
        foreach (var type in LibraryAssembly.GetExportedTypes().Where(t => t.IsEnum).OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            data.Add(type);
        }

        return data;
    }

    /// <summary>
    /// Reported: JsonSerializer.SerializeToElement("reply").Deserialize&lt;MessageLinkType&gt;() threw because
    /// MessageLinkType had no [JsonConverter] attribute (ChatType worked only thanks to case-insensitive parsing).
    /// </summary>
    [Fact]
    public void MessageLinkType_ShouldDeserializeFromApiString_WithDefaultOptions()
    {
        JsonSerializer.SerializeToElement("reply").Deserialize<MessageLinkType>().Should().Be(MessageLinkType.Reply);
        JsonSerializer.SerializeToElement("forward").Deserialize<MessageLinkType>().Should().Be(MessageLinkType.Forward);
        JsonSerializer.Serialize(MessageLinkType.Reply).Should().Be("\"reply\"");
    }

    [Theory]
    [MemberData(nameof(LibraryEnums))]
    public void Enum_ShouldDeclareJsonConverter(Type enumType)
    {
        enumType.GetCustomAttribute<JsonConverterAttribute>().Should().NotBeNull(
            "every public enum must (de)serialize as MAX API strings without MaxJsonSerializer.Options");
    }

    [Theory]
    [MemberData(nameof(LibraryEnums))]
    public void Enum_ShouldRoundTripAsSnakeCaseString_WithDefaultAndLibraryOptions(Type enumType)
    {
        foreach (var value in Enum.GetValues(enumType))
        {
            if (enumType == typeof(UpdateType) && (UpdateType)value == UpdateType.Unknown)
            {
                continue;
            }

            var expectedJson = $"\"{JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString()!)}\"";

            foreach (var options in new[] { DefaultOptions, MaxJsonSerializer.Options })
            {
                JsonSerializer.Serialize(value, enumType, options).Should().Be(expectedJson, $"{enumType.Name}.{value}");
                JsonSerializer.Deserialize(expectedJson, enumType, options).Should().Be(value, $"{enumType.Name}.{value}");
            }

            // PascalCase names (as produced by older library versions) are still accepted.
            JsonSerializer.Deserialize($"\"{value}\"", enumType, DefaultOptions).Should().Be(value);
        }
    }

    [Theory]
    [InlineData("typing_on", ChatAction.TypingOn)]
    [InlineData("mark_seen", ChatAction.MarkSeen)]
    public void ChatAction_ShouldReadSnakeCase_WithDefaultOptions(string wireValue, ChatAction expected)
    {
        JsonSerializer.Deserialize<ChatAction>($"\"{wireValue}\"").Should().Be(expected);
    }

    [Fact]
    public void ChatAdminPermissions_ShouldReadSnakeCase_WithDefaultOptions()
    {
        var permissions = JsonSerializer.Deserialize<ChatAdminPermission[]>("""["read_all_messages","add_remove_members","post_edit_delete_message"]""");

        permissions.Should().Equal(ChatAdminPermission.ReadAllMessages, ChatAdminPermission.AddRemoveMembers, ChatAdminPermission.PostEditDeleteMessage);
    }

    [Fact]
    public void UpdateType_ShouldReadApiString_WithDefaultOptions()
    {
        JsonSerializer.Deserialize<UpdateType>("\"message_callback\"").Should().Be(UpdateType.MessageCallback);
        JsonSerializer.Deserialize<UpdateType>("\"something_new\"").Should().Be(UpdateType.Unknown);
    }

    /// <summary>
    /// Every property of a JSON model must carry an explicit [JsonPropertyName]; otherwise it is mapped only
    /// with MaxJsonSerializer.Options (naming policy) and silently stays empty with any other options.
    /// </summary>
    [Fact]
    public void JsonModels_ShouldDeclarePropertyNamesForEveryProperty()
    {
        var missing = new List<string>();
        foreach (var type in LibraryAssembly.GetExportedTypes().Where(t => t.IsClass && t.Namespace?.StartsWith("Max.Bot.Types", StringComparison.Ordinal) == true))
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray();

            // Only JSON models (types with at least one mapped property) are checked; typed update wrappers are not serialized.
            if (!properties.Any(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() != null))
            {
                continue;
            }

            missing.AddRange(properties
                .Where(p => p.GetCustomAttribute<JsonPropertyNameAttribute>() == null &&
                            p.GetCustomAttribute<JsonIgnoreAttribute>() is not { Condition: JsonIgnoreCondition.Always } &&
                            p.GetCustomAttribute<JsonExtensionDataAttribute>() == null)
                .Select(p => $"{type.Name}.{p.Name}"));
        }

        missing.Should().BeEmpty();
    }

    [Fact]
    public void MessageCreatedUpdate_ShouldBeFullyTyped_WithDefaultOptions()
    {
        var update = JsonSerializer.Deserialize<Update>(MessageCreatedJson)!;

        AssertTypedMessageCreatedUpdate(update);
    }

    [Fact]
    public void MessageCreatedUpdate_ShouldDeserializeTheSame_WithDefaultAndLibraryOptions()
    {
        var withDefault = JsonSerializer.Deserialize<Update>(MessageCreatedJson)!;
        var withLibrary = MaxJsonSerializer.Deserialize<Update>(MessageCreatedJson);

        AssertTypedMessageCreatedUpdate(withLibrary);
        JsonSerializer.Serialize(withDefault, MaxJsonSerializer.Options).Should().Be(JsonSerializer.Serialize(withLibrary, MaxJsonSerializer.Options));
    }

    [Fact]
    public void LinkedMessage_ShouldReturnNullType_ForUnknownLinkType()
    {
        var message = JsonSerializer.Deserialize<Message>("""{"recipient":{"chat_id":1,"chat_type":"secret_chat"},"timestamp":1,"link":{"type":"quote","message":{"mid":"m","seq":1}}}""")!;

        message.Link!.Type.Should().BeNull();
        message.Recipient.ChatType.Should().BeNull();
        message.Link.Mid.Should().Be("m");
    }

    [Fact]
    public void Message_RequiredFields_ShouldBeNonNullable_WhenMissingInJson()
    {
        var message = JsonSerializer.Deserialize<Message>("{}")!;

        message.Recipient.Should().NotBeNull();
        message.Timestamp.Should().Be(0);
        message.Body.Should().BeNull("MAX sends no body for messages that contain only a forwarded message");
    }

    [Fact]
    public void Chat_Participants_ShouldBeTypedDictionary()
    {
        var chat = JsonSerializer.Deserialize<Chat>("""{"chat_id":-1,"type":"chat","status":"active","title":"t","last_event_time":5,"participants_count":2,"is_public":false,"participants":{"10":1730300000000,"20":1730300000001}}""")!;

        chat.Participants.Should().BeEquivalentTo(new Dictionary<long, long> { [10] = 1730300000000, [20] = 1730300000001 });
        chat.LastEventTime.Should().Be(5);
        chat.ParticipantsCount.Should().Be(2);
        chat.Status.Should().Be(ChatStatus.Active);
    }

    [Fact]
    public void BotStartedUpdate_ShouldExposeNonNullableRequiredFields()
    {
        var update = JsonSerializer.Deserialize<Update>("""{"update_type":"bot_started","timestamp":10,"chat_id":77,"user":{"user_id":5,"first_name":"Ann","is_bot":false,"last_activity_time":1},"payload":"ref"}""")!;

        var started = update.BotStartedUpdate!;
        started.ChatId.Should().Be(77);
        started.User.Id.Should().Be(5);
        started.Timestamp.Should().Be(10);
        started.Payload.Should().Be("ref");
    }

    [Fact]
    public void CallbackUpdate_ShouldExposeNonNullableCallbackFields_WithDefaultOptions()
    {
        var update = JsonSerializer.Deserialize<Update>("""{"update_type":"message_callback","timestamp":10,"callback":{"timestamp":11,"callback_id":"cb-1","payload":"yes","user":{"user_id":5,"first_name":"Ann","is_bot":false,"last_activity_time":1}},"message":null}""")!;

        update.Callback!.CallbackId.Should().Be("cb-1");
        update.Callback.User.Id.Should().Be(5);
        update.Callback.Timestamp.Should().Be(11);
        update.Message.Should().BeNull();
    }

    [Fact]
    public void ChatButton_ShouldRoundTrip()
    {
        var button = new InlineKeyboardButton
        {
            Type = ButtonType.Chat,
            Text = "Discuss",
            ChatTitle = "Topic",
            ChatDescription = "About",
            StartPayload = "topic-1",
            Uuid = 42
        };

        var json = MaxJsonSerializer.Serialize(button);
        json.Should().Be("""{"type":"chat","text":"Discuss","chat_title":"Topic","chat_description":"About","start_payload":"topic-1","uuid":42}""");

        var parsed = JsonSerializer.Deserialize<InlineKeyboardButton>(json)!;
        parsed.Should().BeEquivalentTo(button);
    }

    [Fact]
    public void InlineKeyboardAttachment_WithMalformedPayload_ShouldNotBreakMessage()
    {
        var body = JsonSerializer.Deserialize<MessageBody>("""{"mid":"m","seq":1,"attachments":[{"type":"inline_keyboard","payload":{"buttons":"oops"}},{"type":"image","payload":{"photo_id":1,"url":"u","token":"t"}}]}""")!;

        body.Attachments.Should().HaveCount(2);
        body.Attachments![0].Should().BeOfType<InlineKeyboardAttachment>().Which.Buttons.Should().BeEmpty();
        body.Attachments[1].Should().BeOfType<PhotoAttachment>().Which.Url.Should().Be("u");
    }

    [Fact]
    public void AddChatMembersResult_ShouldExposeFailedUsers()
    {
        var result = JsonSerializer.Deserialize<ModifyMembersResponse>("""{"success":false,"message":"partial","failed_user_ids":[3],"failed_user_details":[{"error_code":"add.participant.privacy","user_ids":[3]}]}""")!;

        result.Success.Should().BeFalse();
        result.FailedUserIds.Should().Equal(3L);
        result.FailedUserDetails.Should().ContainSingle().Which.ErrorCode.Should().Be("add.participant.privacy");
    }

    private static void AssertTypedMessageCreatedUpdate(Update update)
    {
        update.Type.Should().Be(UpdateType.MessageCreated);
        update.Timestamp.Should().Be(1730300000000);
        update.ChatId.Should().Be(-42);
        update.ChatType.Should().Be(ChatType.Chat);

        var message = update.Message!;
        message.Recipient.ChatType.Should().Be(ChatType.Chat);
        message.Sender!.FirstName.Should().Be("Ann");
        message.Sender.LastActivityTime.Should().Be(1730300000000);
        message.Stat!.Views.Should().Be(5);

        message.Link!.Type.Should().Be(MessageLinkType.Reply);
        message.Link.Sender!.Id.Should().Be(2);
        message.Link.Message.Mid.Should().Be("mid.original");
        message.Link.Seq.Should().Be(7);

        message.Body!.Mid.Should().Be("mid.reply");
        message.Body.Seq.Should().Be(8);
        message.Body.Markup.Should().ContainSingle().Which.Type.Should().Be(MarkupType.UserMention);

        var keyboard = message.Body.Attachments![0].Should().BeOfType<InlineKeyboardAttachment>().Subject;
        keyboard.CallbackId.Should().Be("kb-1");
        keyboard.Buttons.Should().HaveCount(2);
        keyboard.Buttons[0][0].Type.Should().Be(ButtonType.Callback);
        keyboard.Buttons[0][0].Payload.Should().Be("yes");
        keyboard.Buttons[0][0].Intent.Should().Be(ButtonIntent.Positive);
        keyboard.Buttons[1][0].Type.Should().Be(ButtonType.Chat);
        keyboard.Buttons[1][0].ChatTitle.Should().Be("Topic");
        keyboard.Buttons[1][0].StartPayload.Should().Be("topic-1");
        keyboard.Buttons[1][0].Uuid.Should().Be(123);

        message.Body.Attachments[1].Should().BeOfType<DataAttachment>().Which.Data.Should().Be("reply-keyboard-payload");
    }
}
