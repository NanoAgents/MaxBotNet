using Max.Bot.Types;

namespace Max.Bot.Api;

/// <summary>
/// Interface for bot-related API methods.
/// </summary>
public interface IBotApi
{
    /// <summary>
    /// Gets information about the current bot.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the bot user information.</returns>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxUnauthorizedException">Thrown when authentication fails.</exception>
    Task<User> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about the bot.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the detailed bot information.</returns>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxUnauthorizedException">Thrown when authentication fails.</exception>
    /// <remarks>
    /// <para>WARNING: This endpoint (/bot/info) is not documented in the official MAX API specification and may not be supported by the API.</para>
    /// <para>It is recommended to use <see cref="GetMeAsync"/> instead to get bot information.</para>
    /// </remarks>
    [Obsolete("This endpoint (/bot/info) is not documented in the MAX API specification and may not be supported. Use GetMeAsync() instead to get bot information.")]
    Task<User> GetBotInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the list of bot commands that appear in the command menu.
    /// </summary>
    /// <param name="commands">The collection of commands to set.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the updated bot commands in <see cref="User.Commands"/>; other <see cref="User"/> properties are not returned by this endpoint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when commands is null.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxApiException">Thrown when the API returns an error response.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxNetworkException">Thrown when a network error occurs.</exception>
    /// <exception cref="Max.Bot.Exceptions.MaxUnauthorizedException">Thrown when authentication fails.</exception>
    /// <remarks>
    /// <para>This method uses the PATCH /me/commands endpoint to update bot commands.</para>
    /// <para>See: https://dev.max.ru/docs-api/methods/PATCH/me/commands</para>
    /// </remarks>
    Task<User> SetCommandsAsync(IEnumerable<BotCommand> commands, CancellationToken cancellationToken = default);
}

