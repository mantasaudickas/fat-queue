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
        void RemoveMessage(params int [] messageId);
        void CopyMessageToFailed(int messageId);
        void MoveMessageToEnd(int messageId);
        void Heartbeat(string processName);
        void ClearStaleProcesses(DateTime olderThan);
        void ReleaseProcessLock(params string [] processNames);

        void PurgeCompletedMessages(DateTime olderThan);

        // management
        IEnumerable<MessengerStatus> GetMessengerStatus();
        IEnumerable<QueueStatus> GetQueueStatuses();
        IEnumerable<ProcessStatus> GetActiveProcesses();

        IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize);
        IEnumerable<MessageDetails> GetCompletedMessages(int pageNo, int pageSize);
        IEnumerable<MessageDetails> GetFailedMessages(int pageNo, int pageSize);

        MessageDetails GetMessageDetails(Guid identity);
        void RemoveMessages(params Guid[] identity);
        void ReenqueueFailedMessages(int queueId, params Guid[] identity);
    }
}