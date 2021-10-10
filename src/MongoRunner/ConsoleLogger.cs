using System;
using Microsoft.Extensions.Logging;

namespace MongoRunner
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _level;

        public ConsoleLogger(LogLevel level)
        {
            _level = level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message = "";
            if (formatter != null)
            {
                message += formatter(state, exception);
            }

            var color = Console.ForegroundColor;
            switch (logLevel)
            {
                case LogLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    color = ConsoleColor.Red;
                    break;
                case LogLevel.Critical:
                    color = ConsoleColor.Red;
                    break;
            }

            Console.ForegroundColor = color;
            var logInfo = logLevel.ToString()[..4].ToUpper();
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] [{logInfo}] - {message}");
            Console.ResetColor();
        }

        public void LogDirect(string message, LogLevel logLevel = LogLevel.Information)
        {
            var logInfo = logLevel.ToString()[..4].ToUpper();
            Console.WriteLine($"[{DateTime.Now:hh:mm:ss.fff}] [{logInfo}] - {message}");
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (_level == LogLevel.None) return false;

            return _level <= logLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable();
        }

        private class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}