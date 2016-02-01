using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core
{
    public class Settings
    {
        public bool TraceIsEnabled { get; [UsedImplicitly] set; }
        public ILogger Logger { get; [UsedImplicitly] set; }
        public ISerializer Serializer { get; [UsedImplicitly] set; }
        public Func<Type, string> GetDefaultQueueName { get; [UsedImplicitly] set; }
    }
}
