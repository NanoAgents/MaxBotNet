using System.Text.Json;
using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

/// <summary>
/// Tests attachment deserialization against various JSON formats the API might return.
/// Verifies both flat (expected API format) and nested (backward compat) formats.
/// </summary>
public class AttachmentFormatTests
{
    // ==================== PHOTO / IMAGE ====================

    [Fact]
    public void Photo_FlatFormat_ShouldDeserialize_AllFields()
    {
        // This is the expected real API format: data directly at attachment root
        var json = """{"type":"image","id":123,"file_id":"photo123","width":640,"height":480,"url":"https://example.com/photo.jpg"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)attachment;
        photo.Id.Should().Be(123);
        photo.FileId.Should().Be("photo123");
        photo.Width.Should().Be(640);
        photo.Height.Should().Be(480);
        photo.Url.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public void Photo_NestedFormat_ShouldDeserialize_WithTypeImage()
    {
        // If API wraps in "photo", converter still routes to PhotoAttachment
        var json = """{"type":"image","photo":{"id":456,"file_id":"photo456","width":800,"height":600}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        // Wrapped fields won't deserialize since there's no "id" at root level
        var photo = (PhotoAttachment)attachment;
        photo.Type.Should().Be("image");
    }

    [Fact]
    public void Photo_MinimalFormat_ShouldDeserialize()
    {
        var json = """{"type":"image","file_id":"abc123"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)attachment;
        photo.FileId.Should().Be("abc123");
    }

    [Fact]
    public void Photo_PayloadFormat_ShouldDeserialize_TokenAndUrl()
    {
        var json = """{"type":"image","payload":{"photo_id":14463448907,"token":"token123","url":"https://i.oneme.ru/i?r=abc"}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)attachment;
        photo.Id.Should().Be(14463448907);
        photo.FileId.Should().Be("token123");
        photo.Url.Should().Be("https://i.oneme.ru/i?r=abc");
    }

    // ==================== VIDEO ====================

    [Fact]
    public void Video_NestedFormat_ShouldDeserialize_VideoAttachment()
    {
        var json = """{"type":"video","video":{"id":456,"file_id":"video456","width":1280,"height":720,"duration":60}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<VideoAttachment>();
        var video = (VideoAttachment)attachment;
        video.Id.Should().Be(456);
        video.Duration.Should().Be(60);
    }

    [Fact]
    public void Video_FlatFormat_ShouldDeserialize_VideoAttachment()
    {
        var json = """{"type":"video","id":456,"file_id":"video456","mime_type":"video/mp4"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<VideoAttachment>();
        var video = (VideoAttachment)attachment;
        video.Id.Should().Be(456);
        video.MimeType.Should().Be("video/mp4");
    }

    // ==================== AUDIO ====================

    [Fact]
    public void Audio_NestedFormat_ShouldDeserialize_AudioAttachment()
    {
        var json = """{"type":"audio","audio":{"id":789,"file_id":"audio789","duration":180}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<AudioAttachment>();
        var audio = (AudioAttachment)attachment;
        audio.Id.Should().Be(789);
        audio.Duration.Should().Be(180);
    }

    [Fact]
    public void Audio_FlatFormat_ShouldDeserialize_AudioAttachment()
    {
        var json = """{"type":"audio","id":789,"file_id":"audio789","mime_type":"audio/mpeg"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<AudioAttachment>();
        var audio = (AudioAttachment)attachment;
        audio.Id.Should().Be(789);
    }

    [Fact]
    public void Audio_PayloadFormat_ShouldDeserialize_TokenAndUrl()
    {
        var json = """{"type":"audio","payload":{"token":"voice-token","url":"https://i.oneme.ru/a?r=abc","duration":13}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<AudioAttachment>();
        var audio = (AudioAttachment)attachment;
        audio.FileId.Should().Be("voice-token");
        audio.Url.Should().Be("https://i.oneme.ru/a?r=abc");
        audio.Duration.Should().Be(13);
    }

    // ==================== DOCUMENT ====================

    [Fact]
    public void Document_NestedFormat_ShouldDeserialize_DocumentAttachment()
    {
        var json = """{"type":"file","document":{"id":111,"file_id":"doc111","file_name":"report.pdf"}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<DocumentAttachment>();
        var doc = (DocumentAttachment)attachment;
        doc.Id.Should().Be(111);
        doc.FileName.Should().Be("report.pdf");
    }

    [Fact]
    public void Document_FlatFormat_ShouldDeserialize_DocumentAttachment()
    {
        var json = """{"type":"file","id":111,"file_id":"doc111","file_name":"report.pdf","mime_type":"application/pdf"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<DocumentAttachment>();
        var doc = (DocumentAttachment)attachment;
        doc.Id.Should().Be(111);
        doc.FileName.Should().Be("report.pdf");
        doc.MimeType.Should().Be("application/pdf");
    }

    // ==================== LOCATION ====================

    [Fact]
    public void Location_ShouldDeserialize_Coordinates()
    {
        var json = """{"type":"location","latitude":55.753460,"longitude":37.621602}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<LocationAttachment>();
        var loc = (LocationAttachment)attachment;
        loc.Latitude.Should().Be(55.75346);
        loc.Longitude.Should().Be(37.621602);
    }

    // ==================== CONTACT ====================

    [Fact]
    public void Contact_ShouldDeserialize_VcfAndMaxInfo()
    {
        var json = """{"type":"contact","vcf_info":"BEGIN:VCARD\nTEL:+1234567890\nEND:VCARD","max_info":{"user_id":123,"username":"johndoe"}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.VcfInfo.Should().Contain("BEGIN:VCARD");
        contact.MaxInfo.Should().NotBeNull();
        contact.MaxInfo!.Id.Should().Be(123);
        contact.PhoneNumber.Should().Be("+1234567890");
    }

    // ==================== INLINE KEYBOARD ====================

    [Fact]
    public void InlineKeyboard_ShouldDeserialize_CallbackIdAndPayload()
    {
        var json = """
            {
                "type":"inline_keyboard",
                "callback_id":"cb123",
                "payload":{"buttons":[[{"text":"OK","type":"message"}]]}
            }
            """;

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<InlineKeyboardAttachment>();
        var kb = (InlineKeyboardAttachment)attachment;
        kb.CallbackId.Should().Be("cb123");
        kb.Payload.Should().NotBeNull();
    }

    // ==================== UNKNOWN TYPE ====================

    [Fact]
    public void UnknownType_ShouldDeserialize_AsDocumentAttachment()
    {
        var json = """{"type":"sticker","id":999,"file_id":"sticker1"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<DocumentAttachment>();
    }

    // ==================== FULL MESSAGE ====================

    [Fact]
    public void FullMessage_WithPhotoAttachment_ShouldDeserialize_CompleteMessage()
    {
        var json = """
            {
                "sender":{"id":123,"name":"Test User"},
                "body":{
                    "mid":"msg1",
                    "seq":1,
                    "text":"Check this photo",
                    "attachments":[
                        {"type":"image","id":456,"file_id":"photo456","width":800,"height":600,"url":"https://example.com/pic.jpg"}
                    ]
                }
            }
            """;

        var message = MaxJsonSerializer.Deserialize<Message>(json);

        message.Should().NotBeNull();
        message.Body.Should().NotBeNull();
        message.Body!.Attachments.Should().HaveCount(1);
        message.Body.Attachments![0].Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)message.Body.Attachments[0];
        photo.Id.Should().Be(456);
        photo.Url.Should().Be("https://example.com/pic.jpg");
    }
}
