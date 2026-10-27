using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Types;

namespace Max.Bot.Api;

/// <summary>
/// Implementation of bot-related API methods.
/// </summary>
internal class BotApi : BaseApi, IBotApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BotApi"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or options is null.</exception>
    public BotApi(IMaxHttpClient httpClient, MaxBotOptions options)
        : base(httpClient, options)
    {
    }

    /// <inheritdoc />
    public async Task<User> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, "/me");
        return await ExecuteRequestAsync<User>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    [Obsolete("This endpoint (/bot/info) is not documented in the MAX API specification and may not be supported. Use GetMeAsync() instead to get bot information.")]
    public async Task<User> GetBotInfoAsync(CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, "/bot/info");
        return await ExecuteRequestAsync<User>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<User> SetCommandsAsync(IEnumerable<BotCommand> commands, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var updateRequest = new Types.Requests.UpdateBotInfoRequest
        {
            Commands = commands.ToArray()
        };

        var request = CreateRequest(HttpMethod.Patch, "/me/commands", updateRequest);
        return await ExecuteRequestAsync<User>(request, cancellationToken).ConfigureAwait(false);
    }
}

