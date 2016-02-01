namespace FatQueue.Messenger.Core.Tools
{
    public class TraceLogger : ILogger
    {
        private readonly bool _isTraceEnabled;

        public TraceLogger(bool isTraceEnabled)
        {
            _isTraceEnabled = isTraceEnabled;
        }

        public void Trace(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                System.Diagnostics.Trace.WriteLine(Format(message, arguments), "FatQueue.Messenger.Trace");
        }

        public void Debug(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                System.Diagnostics.Trace.WriteLine(Format(message, arguments), "FatQueue.Messenger.Debug");
        }

        public void Info(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                System.Diagnostics.Trace.WriteLine(Format(message, arguments), "FatQueue.Messenger.Info");
        }

        public void Error(string message, params object[] arguments)
        {
            if (_isTraceEnabled)
                System.Diagnostics.Trace.WriteLine(Format(message, arguments), "FatQueue.Messenger.Error");
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
