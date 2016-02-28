using System;

namespace FatQueue.Messenger.Core
{
    public class Settings
    {
        public bool TraceIsEnabled { get; set; }
        public ILogger Logger { get; set; }
        public ISerializer Serializer { get; set; }
        public Func<Type, string> GetDefaultQueueName { get; set; }
    }
}
