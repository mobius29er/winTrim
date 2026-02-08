using System;
using System.Diagnostics;

namespace WinTrim.Core.Services;

/// <summary>
/// No-op logger implementation for production Mac App Store builds.
/// Console.WriteLine causes crashes in sandboxed environments.
/// Debug builds can use System.Diagnostics.Debug for Xcode console output.
/// </summary>
public sealed class NullLogger : IAppLogger
{
    public void LogInfo(string message)
    {
#if DEBUG
        Debug.WriteLine($"[INFO] {message}");
#endif
    }

    public void LogWarning(string message)
    {
#if DEBUG
        Debug.WriteLine($"[WARNING] {message}");
#endif
    }

    public void LogError(string message, Exception? exception = null)
    {
#if DEBUG
        Debug.WriteLine($"[ERROR] {message}");
        if (exception != null)
        {
            Debug.WriteLine($"[ERROR] Exception: {exception}");
        }
#endif
    }

    public void LogDebug(string message)
    {
#if DEBUG
        Debug.WriteLine($"[DEBUG] {message}");
#endif
    }
}
