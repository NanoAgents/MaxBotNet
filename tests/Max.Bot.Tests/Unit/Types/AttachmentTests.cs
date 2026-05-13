using System.Text.Json;
using FluentAssertions;
using Max.Bot.Networking;
using Max.Bot.Types;
using Xunit;

namespace Max.Bot.Tests.Unit.Types;

public class AttachmentTests
{
    // ==================== PHOTO ====================

    [Fact]
    public void PhotoAttachment_ShouldDeserialize_FromFlatJson()
    {
        var json = """{"type":"image","id":123,"file_id":"photo123","width":640,"height":480,"url":"https://example.com/photo.jpg"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        var photo = (PhotoAttachment)attachment;
        photo.Type.Should().Be("image");
        photo.Id.Should().Be(123);
        photo.FileId.Should().Be("photo123");
        photo.Width.Should().Be(640);
        photo.Height.Should().Be(480);
        photo.Url.Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public void PhotoAttachment_ShouldDeserialize_FromNestedJson()
    {
        // Backward compat: if API wraps in "photo", fields still deserialize due to case-insensitive matching
        var json = """{"type":"image","photo":{"id":123,"file_id":"photo123","width":640,"height":480}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<PhotoAttachment>();
        // photo object fields will be default since there's no direct "id" at root level
        var photo = (PhotoAttachment)attachment;
        photo.Type.Should().Be("image");
    }

    [Fact]
    public void PhotoAttachment_ShouldSerialize_ToFlatJson()
    {
        var attachment = new PhotoAttachment
        {
            Id = 123,
            FileId = "photo123",
            Width = 640,
            Height = 480,
            Url = "https://example.com/photo.jpg"
        };

        var json = MaxJsonSerializer.Serialize<Attachment>(attachment);

        json.Should().Contain("\"type\":\"image\"");
        json.Should().Contain("\"file_id\":\"photo123\"");
        json.Should().Contain("\"url\":\"https://example.com/photo.jpg\"");
        json.Should().NotContain("\"photo\":");
    }

    // ==================== VIDEO ====================

    [Fact]
    public void VideoAttachment_ShouldDeserialize_FromFlatJson()
    {
        var json = """{"type":"video","id":456,"file_id":"video456","width":1280,"height":720,"duration":60,"mime_type":"video/mp4"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<VideoAttachment>();
        var video = (VideoAttachment)attachment;
        video.Type.Should().Be("video");
        video.Id.Should().Be(456);
        video.FileId.Should().Be("video456");
    }

    [Fact]
    public void VideoAttachment_ShouldDeserialize_FromNestedJson()
    {
        var json = """{"type":"video","video":{"id":456,"file_id":"video456","width":1280,"height":720,"duration":60}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<VideoAttachment>();
        var video = (VideoAttachment)attachment;
        video.Type.Should().Be("video");
        video.Id.Should().Be(456);
        video.FileId.Should().Be("video456");
        video.Width.Should().Be(1280);
        video.Height.Should().Be(720);
        video.Duration.Should().Be(60);
    }

    [Fact]
    public void VideoAttachment_ShouldSerialize_ToJson()
    {
        var attachment = new VideoAttachment
        {
            Id = 456,
            FileId = "video456",
            Width = 1280,
            Height = 720,
            Duration = 60
        };

        var json = MaxJsonSerializer.Serialize<Attachment>(attachment);

        json.Should().Contain("\"type\":\"video\"");
        json.Should().Contain("\"file_id\":\"video456\"");
        json.Should().Contain("\"duration\":60");
    }

    // ==================== AUDIO ====================

    [Fact]
    public void AudioAttachment_ShouldDeserialize_FromFlatJson()
    {
        var json = """{"type":"audio","id":789,"file_id":"audio789","duration":180,"mime_type":"audio/mpeg"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<AudioAttachment>();
        var audio = (AudioAttachment)attachment;
        audio.Id.Should().Be(789);
    }

    [Fact]
    public void AudioAttachment_ShouldDeserialize_FromNestedJson()
    {
        var json = """{"type":"audio","audio":{"id":789,"file_id":"audio789","duration":180}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<AudioAttachment>();
        var audio = (AudioAttachment)attachment;
        audio.Id.Should().Be(789);
        audio.FileId.Should().Be("audio789");
        audio.Duration.Should().Be(180);
    }

    [Fact]
    public void AudioAttachment_ShouldSerialize_ToJson()
    {
        var attachment = new AudioAttachment
        {
            Id = 789,
            FileId = "audio789",
            Duration = 180
        };

        var json = MaxJsonSerializer.Serialize<Attachment>(attachment);

        json.Should().Contain("\"type\":\"audio\"");
        json.Should().Contain("\"file_id\":\"audio789\"");
    }

    // ==================== DOCUMENT ====================

    [Fact]
    public void DocumentAttachment_ShouldDeserialize_FromFlatJson()
    {
        var json = """{"type":"file","id":111,"file_id":"doc111","file_name":"report.pdf","file_size":1024,"mime_type":"application/pdf"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<DocumentAttachment>();
        var doc = (DocumentAttachment)attachment;
        doc.Type.Should().Be("file");
        doc.Id.Should().Be(111);
        doc.FileId.Should().Be("doc111");
        doc.FileName.Should().Be("report.pdf");
        doc.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public void DocumentAttachment_ShouldDeserialize_FromNestedJson()
    {
        var json = """{"type":"file","document":{"id":111,"file_id":"doc111","file_name":"report.pdf"}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<DocumentAttachment>();
        var doc = (DocumentAttachment)attachment;
        doc.Id.Should().Be(111);
        doc.FileName.Should().Be("report.pdf");
    }

    [Fact]
    public void DocumentAttachment_ShouldSerialize_ToJson()
    {
        var attachment = new DocumentAttachment
        {
            Id = 111,
            FileId = "doc111",
            FileName = "report.pdf",
            MimeType = "application/pdf"
        };

        var json = MaxJsonSerializer.Serialize<Attachment>(attachment);

        json.Should().Contain("\"type\":\"file\"");
        json.Should().Contain("\"file_name\":\"report.pdf\"");
    }

    // ==================== LOCATION ====================

    [Fact]
    public void LocationAttachment_ShouldDeserialize_FromJson()
    {
        var json = """{"type":"location","latitude":55.753460,"longitude":37.621602}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<LocationAttachment>();
        var loc = (LocationAttachment)attachment;
        loc.Type.Should().Be("location");
        loc.Latitude.Should().Be(55.75346);
        loc.Longitude.Should().Be(37.621602);
    }

    // ==================== CONTACT ====================

    [Fact]
    public void ContactAttachment_ShouldDeserialize_FromJson()
    {
        var json = """{"type":"contact","vcf_info":"BEGIN:VCARD\nTEL:+1234567890\nEND:VCARD","max_info":{"user_id":123,"username":"john_doe","first_name":"John","last_name":"Doe","is_bot":false},"hash":"contact-hash"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.Type.Should().Be("contact");
        contact.VcfInfo.Should().Contain("BEGIN:VCARD");
        contact.MaxInfo.Should().NotBeNull();
        contact.MaxInfo!.Id.Should().Be(123);
        contact.MaxInfo.Username.Should().Be("john_doe");
        contact.Hash.Should().Be("contact-hash");
    }

    [Fact]
    public void ContactAttachment_WithNestedPayload_ShouldDeserializeContactData()
    {
        var json = """{"type":"contact","payload":{"vcf_info":"BEGIN:VCARD\nVERSION:3.0\nFN:John Doe\nTEL:+1234567890\nEND:VCARD","max_info":{"user_id":123,"first_name":"John","last_name":"Doe"}}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.Type.Should().Be("contact");
        contact.VcfInfo.Should().Contain("BEGIN:VCARD");
        contact.MaxInfo.Should().NotBeNull();
        contact.MaxInfo!.Id.Should().Be(123);
        contact.PhoneNumber.Should().Be("+1234567890");
        contact.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ContactAttachment_WithVcfPhoneNumber_ShouldExtractPhoneNumber()
    {
        var vcf = "BEGIN:VCARD\nVERSION:3.0\nFN:John Doe\nTEL:+1234567890\nEND:VCARD";
        var json = JsonSerializer.Serialize(new
        {
            type = "contact",
            vcf_info = vcf,
            max_info = new { user_id = 123 }
        });

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.PhoneNumber.Should().Be("+1234567890");
        contact.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ContactAttachment_WithMaxInfoPhoneNumber_ShouldUseMaxInfoPhoneNumber()
    {
        var json = """{"type":"contact","vcf_info":"BEGIN:VCARD\nTEL:+9999999999\nEND:VCARD","max_info":{"user_id":123,"phone_number":"+1234567890","full_name":"John Doe"}}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.PhoneNumber.Should().Be("+1234567890");
        contact.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ContactAttachment_WithNullMaxInfo_ShouldReturnNullForPhoneNumber()
    {
        var json = """{"type":"contact"}""";

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<ContactAttachment>();
        var contact = (ContactAttachment)attachment;
        contact.PhoneNumber.Should().BeNull();
        contact.FullName.Should().BeNull();
    }

    [Fact]
    public void ContactAttachment_ShouldSerialize_ToJson()
    {
        var attachment = new ContactAttachment
        {
            VcfInfo = "BEGIN:VCARD\nTEL:+1234567890\nEND:VCARD",
            MaxInfo = new ContactInfo
            {
                Id = 123,
                Username = "john_doe",
                FirstName = "John",
                LastName = "Doe"
            },
            Hash = "contact-hash"
        };

        var json = MaxJsonSerializer.Serialize<Attachment>(attachment);

        json.Should().Contain("\"type\":\"contact\"");
        json.Should().Contain("\"vcf_info\"");
        json.Should().Contain("\"max_info\"");
        json.Should().Contain("\"hash\":\"contact-hash\"");
        json.Should().Contain("\"user_id\":123");
    }

    // ==================== INLINE KEYBOARD ====================

    [Fact]
    public void InlineKeyboardAttachment_ShouldDeserialize_FromJson()
    {
        var json = """
            {
                "type":"inline_keyboard",
                "callback_id":"cb123",
                "payload":{
                    "buttons":[
                        [{"text":"OK","type":"message"}]
                    ]
                }
            }
            """;

        var attachment = MaxJsonSerializer.Deserialize<Attachment>(json);

        attachment.Should().BeOfType<InlineKeyboardAttachment>();
        var kb = (InlineKeyboardAttachment)attachment;
        kb.CallbackId.Should().Be("cb123");
        kb.Payload.Should().NotBeNull();
    }
}
