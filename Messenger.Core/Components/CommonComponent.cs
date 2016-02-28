using System;
using FatQueue.Messenger.Core.Tools;

namespace FatQueue.Messenger.Core.Components
{
    public abstract class CommonComponent
    {
        protected internal const string DefaultQueueName = "FatQueue.Messenger.Default";

        protected ILogger Logger { get; private set; }
        protected ISerializer Serializer { get; private set; }
        protected Func<Type, string> GetDefaultQueueName { get; private set; }

        protected CommonComponent(Settings settings)
        {
            Logger = settings.Logger != null
                ? (ILogger) new ConditionalLogger(settings.Logger, settings.TraceIsEnabled)
                : new TraceLogger(settings.TraceIsEnabled);

            Serializer = settings.Serializer ?? new JsonSerializer();
            GetDefaultQueueName = settings.GetDefaultQueueName ?? (type => DefaultQueueName);
        }
    }
}
