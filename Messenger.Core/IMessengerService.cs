using System;
using System.Collections.Generic;

namespace FatQueue.Messenger.Core
{
    public interface IMessengerService
    {
        IEnumerable<MessengerStatus> GetMessengerStatus();
        IEnumerable<QueueStatus> GetQueueStatuses();
        IEnumerable<ProcessStatus> GetActiveProcesses();

        IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize);
        IEnumerable<MessageDetails> GetCompletedMessages(int pageNo, int pageSize);
        IEnumerable<MessageDetails> GetFailedMessages(int pageNo, int pageSize);

        MessageDetails GetMessageDetails(Guid identity);
        void RemoveMessages(params Guid[] identity);

        int ReenqueueFailedMessages();
        int ReenqueueFailedMessages(string queueName, params Guid[] identity);

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

    public class MessageDetails
    {
        public int MessageId { get; set; }
        public int? QueueId { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Context { get; set; }
        public Guid Identity { get; set; }
        public string Error { get; set; }
        public string State { get; set; }
    }
}
