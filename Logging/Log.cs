using System;
using Microsoft.Extensions.Logging;

namespace StudioCCS.Logging
{
    /// <summary>
    /// Thin static facade over Microsoft.Extensions.Logging. The framework owns
    /// levels, filtering, formatting, and the console sink; this type just gives
    /// the (DI-less) codebase a static entry point in the existing utility-class
    /// idiom and folds in the only behaviour we actually care about beyond plain
    /// logging: optional per-message de-duplication (see <see cref="LogOnce"/>).
    /// </summary>
    public static class Log
    {
        private static ILoggerFactory _factory;
        private static ILogger _logger;

        /// <summary>
        /// Builds the logger factory and the single shared logger. Call once at
        /// startup before anything logs; until then the facade is a safe no-op
        /// (mirrors the old null-sink behaviour).
        /// </summary>
        public static void Init(Action<ILoggingBuilder> configure)
        {
            _factory = LoggerFactory.Create(configure);
            _logger = _factory.CreateLogger("StudioCCS");
        }

        public static void Error(string message, bool once = false)
        {
            if (_logger == null || !ShouldLog(message, once))
            {
                return;
            }

            _logger.LogError(Normalize(message));
        }

        public static void Warning(string message, bool once = false)
        {
            if (_logger == null || !ShouldLog(message, once))
            {
                return;
            }

            _logger.LogWarning(Normalize(message));
        }

        public static void Info(string message, bool once = false)
        {
            if (_logger == null || !ShouldLog(message, once))
            {
                return;
            }

            _logger.LogInformation(Normalize(message));
        }

        // Trailing newlines used to be baked into every call site; strip them here
        // so each sink owns its own line termination (the console provider and the
        // panel provider both append one).
        private static string Normalize(string message) => message?.TrimEnd('\r', '\n');

        private static bool ShouldLog(string message, bool once) => !once || LogOnce.FirstTime(message);
    }
}
