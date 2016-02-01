using System;

namespace FatQueue.Messenger.Core.Tools
{
    public class ConditionalLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly bool _isTraceEnabled;

        public ConditionalLogger(ILogger logger, bool isTraceEnabled)
        {
            if (logger == null) throw new ArgumentNullException("logger");
            _logger = logger;
            _isTraceEnabled = isTraceEnabled;
        }

        public void Trace(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
            {
                _logger.Trace(message, arguments);
            }
        }

        public void Debug(string message, params object[] arguments)
        {
            _logger.Debug(message, arguments);
        }

        public void Info(string message, params object[] arguments)
        {
            _logger.Info(message, arguments);
        }

        public void Error(string message, params object[] arguments)
        {
            _logger.Error(message, arguments);
        }
    }
}
