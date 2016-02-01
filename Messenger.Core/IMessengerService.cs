using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerService
    {
        [UsedImplicitly]
        IEnumerable<QueueStatus> GetQueueStatuses();

        [UsedImplicitly]
        IEnumerable<MessengerStatus> GetMessengerStatus();

        [UsedImplicitly]
        IEnumerable<ProcessStatus> GetActiveProcesses();

        [UsedImplicitly]
        IEnumerable<FailedMessage> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to);

        [UsedImplicitly]
        void ReenqueueFailedMessages(int[] ids, string queueName = null);

        void RecoverFailedMessages();

        void ReleaseProcessLock(string processName);
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class QueueStatus
    {
        public int QueueId { get; set; }
        public string Name { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? ProcessingStarted { get; set; }
        public string ProcessName { get; set; }
        public int? Retries { get; set; }
        public DateTime? NextTryTime { get; set; }
        public string Error { get; set; }
        public int MessageCount { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class MessengerStatus
    {
        public string Status { get; set; }
        public int MessageCount { get; set; }

        public override string ToString()
        {
            return $"{Status}: {MessageCount}";
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class ProcessStatus
    {
        public string ProcessName { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class FailedMessage
    {
        public int FailedMessageId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
        public string Context { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime FailedDate { get; set; }
    }
}
