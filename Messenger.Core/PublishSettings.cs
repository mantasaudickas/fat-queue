using System;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core
{
    public class PublishSettings
    {
        public int MaxRetriesBeforeFail { get; set; }
        public RecoveryMode RecoveryMode { get; set; }
        public TransactionIsolationLevel JobIsolationLevel { get; set; }
        public TimeSpan JobTimeout { get; set; }
        public bool DiscardWhenComplete { get; set; }

        public static readonly PublishSettings Default = new PublishSettings
        {
            MaxRetriesBeforeFail = 2,
            RecoveryMode = RecoveryMode.MarkAsFailed,
            JobIsolationLevel = TransactionIsolationLevel.ReadCommitted,
            JobTimeout = TimeSpan.FromMinutes(10),
            DiscardWhenComplete = false
        };
    }

    /// <summary>
    /// Specifies the isolation level of a transaction.
    /// </summary>
    /// <remarks>
    /// Enum is copied from System.Transactions just to avoid reference to it for clients using this component
    /// </remarks>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
