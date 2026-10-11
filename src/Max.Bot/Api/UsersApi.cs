using System.Globalization;
using System.Net.Http;
using Max.Bot.Configuration;
using Max.Bot.Networking;
using Max.Bot.Types;

namespace Max.Bot.Api;

/// <summary>
/// Implementation of user-related API methods.
/// </summary>
internal class UsersApi : BaseApi, IUsersApi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApi"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests.</param>
    /// <param name="options">The bot options containing token and base URL.</param>
    /// <exception cref="ArgumentNullException">Thrown when httpClient or options is null.</exception>
    public UsersApi(IMaxHttpClient httpClient, MaxBotOptions options)
        : base(httpClient, options)
    {
    }

    /// <inheritdoc />
    [Obsolete("This endpoint (/users/{userId}) is not documented in the MAX API specification and may not be supported.")]
    public async Task<User> GetUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);

        var request = CreateRequest(HttpMethod.Get, string.Create(CultureInfo.InvariantCulture, $"/users/{userId}"));
        return await ExecuteRequestAsync<User>(request, cancellationToken).ConfigureAwait(false);
    }
}

