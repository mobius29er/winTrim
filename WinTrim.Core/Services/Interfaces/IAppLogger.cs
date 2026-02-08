namespace WinTrim.Core.Services;

/// <summary>
/// Simple application logger interface for sandbox-safe logging.
/// Implementations can write to platform-specific log systems (ASL on macOS, Event Log on Windows)
/// or be no-op for production releases.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message
    /// </summary>
    void LogError(string message, Exception? exception = null);

    /// <summary>
    /// Logs a debug message (only in debug builds)
    /// </summary>
    void LogDebug(string message);
}
