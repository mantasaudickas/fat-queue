using System;

namespace FatQueue.Messenger.Core
{
    public class PublishSettings
    {
        public RecoveryMode RecoveryMode { get; set; }
        public TransactionIsolationLevel JobIsolationLevel { get; set; }
        public TimeSpan JobTimeout { get; set; }
        public int MaxRetriesBeforeFail { get; set; }
        public int DelayExecutionInSeconds { get; set; }
        public bool DiscardWhenComplete { get; set; }
        public bool HighestPriority { get; set; }
        public Guid? Identity { get; set; }

        public PublishSettings()
        {
            RecoveryMode = RecoveryMode.MarkAsFailed;
            JobIsolationLevel = TransactionIsolationLevel.ReadCommitted;
            JobTimeout = TimeSpan.FromMinutes(10);
            MaxRetriesBeforeFail = 2;
            DelayExecutionInSeconds = 0;
            DiscardWhenComplete = false;
            HighestPriority = false;
            Identity = null;
        }
    }

    /// <summary>
    /// Specifies the isolation level of a transaction.
    /// </summary>
    /// <remarks>
    /// Enum is copied from System.Transactions just to avoid reference to it for clients using this component
    /// </remarks>
    public enum TransactionIsolationLevel
    {
        Serializable,
        RepeatableRead,
        ReadCommitted,
        ReadUncommitted,
        Snapshot,
        Chaos,
        Unspecified,
    }
}
