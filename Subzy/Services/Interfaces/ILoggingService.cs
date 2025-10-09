namespace Subzy.Services.Interfaces;

/// <summary>
/// Interface for centralized logging service.
/// </summary>
public interface ILoggingService
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void Debug(string message, Exception? exception = null);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Info(string message, Exception? exception = null);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void Warning(string message, Exception? exception = null);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    string GetLogFilePath();

    /// <summary>
    /// Clears all log files.
    /// </summary>
    Task ClearLogsAsync();
}
