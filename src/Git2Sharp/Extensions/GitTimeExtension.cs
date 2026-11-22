using System;
using Git2Sharp.Bindings;

namespace Git2Sharp.Extensions;

internal static class GitTimeExtension
{
    /// <summary>
    /// Converts a GitTime struct into a managed DateTime instance.
    /// </summary>
    /// <param name="time">The GitTime struct containing time information.</param>
    /// <returns>A DateTime instance with the same time representation</returns>
    internal static DateTime ToDateTime(this GitTime time)
    {
        DateTimeOffset utc = DateTimeOffset.FromUnixTimeSeconds(time.Time);
        int offset = time.Offset;
        if (time.Sign == '-')
        {
            offset = -offset;
        }

        return utc.ToOffset(TimeSpan.FromMinutes(offset)).DateTime;
    }
}