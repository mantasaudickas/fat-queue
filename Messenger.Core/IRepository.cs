using System;
using System.Collections.Generic;
using FatQueue.Messenger.Core.Data;
using FatQueue.Messenger.Core.Orm;

namespace FatQueue.Messenger.Core
{
    public interface IRepository
    {
        ITransaction CreateProcessingTransaction(TransactionIsolationLevel jobTransactionIsolationLevel, TimeSpan jobTimeout);
        int? FetchQueueId(string queueName);
        int CreateQueue(string queueName);
        void DeleteQueue(int queueId);
        void CreateMessage(int queueId, string contentType, string content, string context, int delayInSeconds, Guid? identity);
        void InsertMessage(int queueId, string contentType, string content, string context, Guid? identity);
        QueueInfo LockQueue(string processName);
        void ReleaseQueue(int queueId);
        void SetQueueFailure(int queueId, int retries, string formattedError, DateTime nextTryTime);
        IList<MessageInfo> FetchQueueMessages(int queueId, int messageCount);
        int CancelMessages(Guid identity);
        void RemoveMessage(int messageId, bool archiveMessage);
        void RemoveMessage(int messageId);
        void CopyMessageToFailed(int messageId);
        void MoveMessageToEnd(int messageId);
        void Heartbeat(string processName);
        void ClearStaleProcesses(DateTime olderThan);
        void ReleaseProcessLock(params string [] processNames);
        IEnumerable<QueueStatus> GetQueueStatuses();
        IEnumerable<MessengerStatus> GetMessengerStatus();
        IEnumerable<ProcessStatus> GetActiveProcesses();
        void PurgeCompletedMessages(DateTime olderThan);
        void ReenqueueFailedMessages(int queueId, int[] ids);
        IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize, DateTime? @from, DateTime? to);
        IEnumerable<CompletedMessageDetails> GetCompletedMessages(int pageNo, int pageSize, DateTime? @from, DateTime? to);
        IEnumerable<FailedMessageDetails> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to);
        MessageDetails GetMessage(int messageId);
        CompletedMessageDetails GetCompletedMessage(int messageId);
        FailedMessageDetails GetFailedMessage(int messageId);
    }
}