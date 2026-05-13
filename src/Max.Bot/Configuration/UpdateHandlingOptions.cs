using System;
using System.Collections.Generic;
using Max.Bot.Types.Enums;

namespace Max.Bot.Configuration;

/// <summary>
/// Defines dispatcher-level knobs inspired by Telegram.Bot and VkNet handler pipelines.
/// </summary>
public sealed class UpdateHandlingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of updates that may be processed in parallel.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets a value indicating whether the dispatcher must preserve update ordering (uses sequential execution when true).
    /// </summary>
    public bool PreserveUpdateOrder { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout applied to a single handler invocation.
    /// </summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets a value indicating whether handler exceptions bubble up (causing retries) or are logged and swallowed.
    /// </summary>
    public bool PropagateHandlerExceptions { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional list of user names allowed to trigger handlers (mirrors Telegram.Bot integration test filters).
    /// </summary>
    public ICollection<string> AllowedUsernames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the update types that should be dispatched.
    /// </summary>
    public ICollection<UpdateType> AllowedUpdateTypes { get; set; } = new List<UpdateType>();

    /// <summary>
    /// Validates option values.
    /// </summary>
    public void Validate()
    {
        if (MaxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism), MaxDegreeOfParallelism, "MaxDegreeOfParallelism must be at least 1.");
        }

        if (HandlerTimeout <= TimeSpan.Zero && HandlerTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(HandlerTimeout), HandlerTimeout, "HandlerTimeout must be greater than zero or Timeout.InfiniteTimeSpan.");
        }

        if (HandlerTimeout != Timeout.InfiniteTimeSpan && HandlerTimeout > TimeSpan.FromMilliseconds(int.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(HandlerTimeout), HandlerTimeout, "HandlerTimeout must be less than or equal to int.MaxValue milliseconds or Timeout.InfiniteTimeSpan.");
        }

        if (AllowedUsernames is null)
        {
            throw new ArgumentNullException(nameof(AllowedUsernames), "AllowedUsernames cannot be null. Use an empty array to disable filtering.");
        }

        if (AllowedUpdateTypes is null)
        {
            throw new ArgumentNullException(nameof(AllowedUpdateTypes), "AllowedUpdateTypes cannot be null. Use an empty array to disable filtering.");
        }
    }
}



