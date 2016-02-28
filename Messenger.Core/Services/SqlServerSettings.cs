using System;
using FatQueue.Messenger.Core.Database;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core.Services
{
    public class SqlServerSettings : SqlSettings
    {
        [UsedImplicitly]
        public IJobActivator JobActivator { get; set; }
        public TimeSpan? CheckInterval { get; set; }

        public int MaxProcessCount { get; set; }
        public int MessageBatchSize { get; set; }

        public ProcessNameFormat ProcessNameFormat { get; set; }
        public string CustomProcessName { get; set; }

        public CompletedMessages CompletedMessages { get; set; }
    }

    public class CompletedMessages
    {
        public bool Archive { get; set; }
        public bool Cleanup { get; set; }
        public Func<DateTime> CleanOlderThanUtc { get; set; }

        public static CompletedMessages Default
        {
            get
            {
                return new CompletedMessages {Archive = false, Cleanup = false, CleanOlderThanUtc = () => DateTime.Today};
            }
        }
    }
}
