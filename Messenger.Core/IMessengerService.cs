using System;
using System.Collections.Generic;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerService
    {
        IEnumerable<QueueStatus> GetQueueStatuses();

        IEnumerable<MessengerStatus> GetMessengerStatus();

        IEnumerable<ProcessStatus> GetActiveProcesses();

        IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize, DateTime? from, DateTime? to);

        IEnumerable<CompletedMessageDetails> GetCompletedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to);

        IEnumerable<FailedMessageDetails> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to);

        MessageDetails GetMessage(int messageId);

        CompletedMessageDetails GetCompletedMessage(int messageId);

        FailedMessageDetails GetFailedMessage(int messageId);

        void ReenqueueFailedMessages(int[] ids, string queueName = null);

        void RecoverFailedMessages();

        void ReleaseProcessLock(string processName);
    }

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

    public class MessengerStatus
    {
        public string Status { get; set; }
        public int MessageCount { get; set; }

        public override string ToString()
        {
            return $"{Status}: {MessageCount}";
        }
    }

    public class ProcessStatus
    {
        public string ProcessName { get; set; }
        public DateTime LastHeartbeat { get; set; }
    }

    public class FailedMessageDetails
    {
        public int FailedMessageId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public DateTime CreateDate { get; set; }
        public string Error { get; set; }
        public string Context { get; set; }
        public DateTime FailedDate { get; set; }
        public Guid? Identity { get; set; }
    }

    public class CompletedMessageDetails
    {
        public int CompletedMessageId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public string Context { get; set; }
        public Guid? Identity { get; set; }
    }

    public class MessageDetails
    {
        public int MessageId { get; set; }
        public int QueueId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public DateTime StartDate { get; set; }
        public string Context { get; set; }
        public Guid? Identity { get; set; }
    }
}
