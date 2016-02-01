using System;

namespace FatQueue.Messenger.Core.Tools
{
    public class ConsoleLogger : ILogger
    {
        private readonly bool _isTraceEnabled;

        public ConsoleLogger(bool isTraceEnabled)
        {
            _isTraceEnabled = isTraceEnabled;
        }

        public void Trace(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                Console.WriteLine("TRACE: {0}", Format(message, arguments));
        }

        public void Debug(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                Console.WriteLine("DEBUG: {0}", Format(message, arguments));
        }

        public void Info(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                Console.WriteLine("INFO: {0}", Format(message, arguments));
        }

        public void Error(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                Console.WriteLine("ERROR: {0}", Format(message, arguments));
        }

        private static string Format(string message, params object[] arguments)
        {
            if (arguments != null && arguments.Length > 0)
            {
                message = string.Format(message, arguments);
            }
            return message;
        }
    }
}
