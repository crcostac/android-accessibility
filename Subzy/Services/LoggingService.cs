using Microsoft.Extensions.Logging;
using Subzy.Services.Interfaces;
using System.Text;

namespace Subzy.Services;

/// <summary>
/// Centralized logging service with file output and log rotation.
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly string _logDirectory;
    private readonly string _logFileName;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private const long MaxLogFileSize = 5 * 1024 * 1024; // 5 MB

    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger;
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        _logFileName = $"subzy_{DateTime.Now:yyyyMMdd}.log";
        
        Directory.CreateDirectory(_logDirectory);
    }

    public void Debug(string message, Exception? exception = null)
    {
        _logger.LogDebug(exception, message);
        WriteToFile("DEBUG", message, exception);
    }

    public void Info(string message, Exception? exception = null)
    {
        _logger.LogInformation(exception, message);
        WriteToFile("INFO", message, exception);
    }

    public void Warning(string message, Exception? exception = null)
    {
        _logger.LogWarning(exception, message);
        WriteToFile("WARNING", message, exception);
    }

    public void Error(string message, Exception? exception = null)
    {
        _logger.LogError(exception, message);
        WriteToFile("ERROR", message, exception);
    }

    public string GetLogFilePath()
    {
        return Path.Combine(_logDirectory, _logFileName);
    }

    public async Task ClearLogsAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var logFiles = Directory.GetFiles(_logDirectory, "*.log");
            foreach (var file in logFiles)
            {
                File.Delete(file);
            }
            Info("Log files cleared");
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void WriteToFile(string level, string message, Exception? exception)
    {
        Task.Run(async () =>
        {
            await _fileLock.WaitAsync();
            try
            {
                var logFile = GetLogFilePath();
                
                // Check for log rotation
                if (File.Exists(logFile) && new FileInfo(logFile).Length > MaxLogFileSize)
                {
                    RotateLog(logFile);
                }

                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.Message}");
                    logEntry.AppendLine($"StackTrace: {exception.StackTrace}");
                }

                await File.AppendAllTextAsync(logFile, logEntry.ToString());
            }
            catch
            {
                // Silently fail to avoid infinite loops
            }
            finally
            {
                _fileLock.Release();
            }
        });
    }

    private void RotateLog(string currentLogFile)
    {
        try
        {
            var archiveName = $"{Path.GetFileNameWithoutExtension(currentLogFile)}_{DateTime.Now:HHmmss}.log";
            var archivePath = Path.Combine(_logDirectory, archiveName);
            File.Move(currentLogFile, archivePath);

            // Keep only last 10 log files
            var allLogs = Directory.GetFiles(_logDirectory, "*.log")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();

            foreach (var oldLog in allLogs.Skip(10))
            {
                File.Delete(oldLog);
            }
        }
        catch
        {
            // Silently fail
        }
    }
}
