using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Types;
using Max.Bot.Types.Requests;

namespace Max.Bot.Api;

/// <summary>
/// Implementation of chat-related API methods.
/// </summary>
internal class ChatsApi : BaseApi, IChatsApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatsApi"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or options is null.</exception>
    public ChatsApi(IMaxHttpClient httpClient, MaxBotOptions options)
        : base(httpClient, options)
    {
    }

    /// <inheritdoc />
    public async Task<Chat> GetChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}"));
        return await ExecuteRequestAsync<Chat>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Chat[]> GetChatsAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetChatListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Chats ?? Array.Empty<Chat>();
    }

    /// <inheritdoc />
    public async Task<ChatListResponse> GetChatListAsync(int? count = null, long? marker = null, CancellationToken cancellationToken = default)
    {
        if (count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be between 1 and 100.");
        }

        var queryParams = new Dictionary<string, string?>();
        if (count.HasValue)
        {
            queryParams["count"] = count.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (marker.HasValue)
        {
            queryParams["marker"] = marker.Value.ToString(CultureInfo.InvariantCulture);
        }

        // * GET /chats returns {"chats":[...],"marker":...}
        var request = CreateRequest(HttpMethod.Get, "/chats", null, queryParams.Count > 0 ? queryParams : null);
        var response = await ExecuteRequestAsync<ChatListResponse>(request, cancellationToken).ConfigureAwait(false);
        response.Chats ??= Array.Empty<Chat>();
        return response;
    }

    /// <inheritdoc />
    public async Task<Chat> GetChatByLinkAsync(string chatLink, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chatLink))
        {
            throw new ArgumentException("Chat link cannot be null or empty.", nameof(chatLink));
        }

        var request = CreateRequest(HttpMethod.Get, $"/chats/{Uri.EscapeDataString(chatLink)}");
        return await ExecuteRequestAsync<Chat>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Chat> UpdateChatAsync(long chatId, UpdateChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiRequest = CreateRequest(HttpMethod.Patch, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}"), request);
        return await ExecuteRequestAsync<Chat>(apiRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Response> DeleteChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Delete, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}"));
        return await ExecuteRequestAsync<Response>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Response> SendChatActionAsync(long chatId, SendChatActionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiRequest = CreateRequest(HttpMethod.Post, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/actions"), request);
        return await ExecuteRequestAsync<Response>(apiRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Message?> GetPinnedMessageAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/pin"));

        // * GET /chats/{chatId}/pin returns {"message": {...}} or {"message": null} when nothing is pinned.
        var responseBody = await HttpClient.SendAsyncRaw(request, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            // Legacy {"ok": true, "result": {...}} envelope.
            if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
            {
                return result.Deserialize<Message>(MaxJsonSerializer.Options);
            }

            return root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.Object
                ? message.Deserialize<Message>(MaxJsonSerializer.Options)
                : null;
        }
        catch (JsonException ex)
        {
            throw new Exceptions.MaxApiException(
                $"Failed to parse pinned message response: {ex.Message}",
                null,
                System.Net.HttpStatusCode.BadRequest,
                ex);
        }
    }

    /// <inheritdoc />
    public async Task<Response> PinMessageAsync(long chatId, PinMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiRequest = CreateRequest(HttpMethod.Put, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/pin"), request);
        return await ExecuteRequestAsync<Response>(apiRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Response> UnpinMessageAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Delete, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/pin"));
        return await ExecuteRequestAsync<Response>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChatMember> GetChatMembershipAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members/me"));
        return await ExecuteRequestAsync<ChatMember>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Response> LeaveChatAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Delete, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members/me"));
        return await ExecuteRequestAsync<Response>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChatMember[]> GetChatAdminsAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members/admins"));
        var response = await ExecuteRequestAsync<ChatMembersResponse>(request, cancellationToken).ConfigureAwait(false);
        return response.Members ?? [];
    }

    /// <inheritdoc />
    public async Task<Response> AddChatAdminAsync(long chatId, AddChatAdminRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiRequest = CreateRequest(HttpMethod.Post, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members/admins"), request);
        return await ExecuteRequestAsync<Response>(apiRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Response> RemoveChatAdminAsync(long chatId, long userId, CancellationToken cancellationToken = default)
    {
        ValidateUserIdStrict(userId);

        var request = CreateRequest(HttpMethod.Delete, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members/admins/{userId}"));
        return await ExecuteRequestAsync<Response>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChatMember[]> GetChatMembersAsync(long chatId, long? marker = null, int? count = null, CancellationToken cancellationToken = default)
    {
        var response = await GetChatMembersPageAsync(chatId, marker, count, cancellationToken).ConfigureAwait(false);
        return response.Members ?? [];
    }

    /// <inheritdoc />
    public async Task<ChatMembersResponse> GetChatMembersPageAsync(long chatId, long? marker = null, int? count = null, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string?>();
        if (marker.HasValue)
        {
            queryParams["marker"] = marker.Value.ToString(CultureInfo.InvariantCulture);
        }
        if (count.HasValue)
        {
            queryParams["count"] = count.Value.ToString(CultureInfo.InvariantCulture);
        }

        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members"), null, queryParams.Count > 0 ? queryParams : null);
        var response = await ExecuteRequestAsync<ChatMembersResponse>(request, cancellationToken).ConfigureAwait(false);
        response.Members ??= [];
        return response;
    }

    /// <inheritdoc />
    public async Task<ChatMember[]> GetChatMembersByIdsAsync(long chatId, IEnumerable<long> userIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids = userIds.Distinct().Select(id => id.ToString(CultureInfo.InvariantCulture)).ToArray();
        if (ids.Length == 0)
        {
            throw new ArgumentException("At least one user ID must be provided.", nameof(userIds));
        }

        var queryParams = new Dictionary<string, string?>
        {
            { "user_ids", string.Join(",", ids) }
        };

        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members"), null, queryParams);
        var response = await ExecuteRequestAsync<ChatMembersResponse>(request, cancellationToken).ConfigureAwait(false);
        return response.Members ?? [];
    }

    /// <inheritdoc />
    public async Task<ModifyMembersResponse> AddChatMembersAsync(long chatId, AddChatMembersRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiRequest = CreateRequest(HttpMethod.Post, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members"), request);
        return await ExecuteRequestAsync<ModifyMembersResponse>(apiRequest, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Response> RemoveChatMemberAsync(long chatId, long userId, CancellationToken cancellationToken = default)
    {
        return RemoveChatMemberCoreAsync(chatId, userId, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Response> RemoveChatMemberAsync(long chatId, long userId, bool block, CancellationToken cancellationToken = default)
    {
        return RemoveChatMemberCoreAsync(chatId, userId, block, cancellationToken);
    }

    private async Task<Response> RemoveChatMemberCoreAsync(long chatId, long userId, bool? block, CancellationToken cancellationToken)
    {
        ValidateUserIdStrict(userId);

        var queryParams = new Dictionary<string, string?>
        {
            { "user_id", userId.ToString(CultureInfo.InvariantCulture) }
        };

        if (block == true)
        {
            queryParams["block"] = "true";
        }

        var request = CreateRequest(HttpMethod.Delete, string.Create(CultureInfo.InvariantCulture, $"/chats/{chatId}/members"), null, queryParams);
        return await ExecuteRequestAsync<Response>(request, cancellationToken).ConfigureAwait(false);
    }
}
