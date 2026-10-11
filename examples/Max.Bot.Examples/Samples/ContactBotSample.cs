using System;
using System.Threading;
using System.Threading.Tasks;
using Max.Bot.Polling;
using Max.Bot.Types;
using Max.Bot.Types.Enums;

namespace Max.Bot.Examples.Samples;

/// <summary>
/// Sample showcasing contact handling.
/// Demonstrates how to extract phone numbers and contact information from contact attachments.
/// </summary>
public sealed class ContactBotSample : IBotSample
{
    /// <inheritdoc />
    public string Name => "contact";

    /// <inheritdoc />
    public string Description => "Handles contact sharing and extracts phone numbers.";

    /// <inheritdoc />
    public Task RunAsync(SampleExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handler = new DelegatingUpdateHandler(
            onMessage: (updateContext, ct) => HandleMessageAsync(context, updateContext, ct));

        return SampleUtilities.RunPollingLoopAsync(context, handler, cancellationToken);
    }

    private static async Task HandleMessageAsync(SampleExecutionContext sampleContext, UpdateContext context, CancellationToken cancellationToken)
    {
        var message = context.Update.Message;
        var chatId = SampleUtilities.GetChatId(message);

        if (!chatId.HasValue)
        {
            return;
        }

        // Check if message has attachments
#pragma warning disable CS8602
        var attachments = message.Body?.Attachments! ?? Array.Empty<Attachment>();
#pragma warning restore CS8602
        if (attachments.Length == 0)
        {
            return;
        }

        // Look for contact attachment
        foreach (var attachment in attachments)
        {
            if (attachment is ContactAttachment contactAttachment)
            {
                await HandleContactAsync(sampleContext, context, contactAttachment, chatId.Value, cancellationToken);
                return;
            }
        }
    }

    private static async Task HandleContactAsync(
        SampleExecutionContext sampleContext,
        UpdateContext context,
        ContactAttachment contactAttachment,
        long chatId,
        CancellationToken cancellationToken)
    {
        // Method 1: Use helper properties (recommended)
        var phoneNumber = contactAttachment.PhoneNumber;
        var fullName = contactAttachment.FullName;

        // Method 2: Access raw data
        var vcfInfo = contactAttachment.VcfInfo;
        var maxInfo = contactAttachment.MaxInfo;

        // Method 3: Use ContactHelpers directly
        var parsedPhoneNumber = ContactHelpers.GetPhoneNumber(contactAttachment);
        var parsedFullName = ContactHelpers.GetFullName(contactAttachment);

        var response = $"""
            Новый контакт получен! 📇

            Телефон: {phoneNumber ?? "Не указан"}
            Имя: {fullName ?? "Не указано"}

            Дополнительная информация:
            - User ID: {maxInfo?.Id}
            - Username: {maxInfo?.Username ?? "Нет"}
            - Bot: {(maxInfo?.IsBot == true ? "Да" : "Нет")}
            """;

        await context.Api.Messages.SendMessageAsync(
            chatId,
            response,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        sampleContext.Output.WriteLine($"Contact received: {phoneNumber}, {fullName}");
    }
}
