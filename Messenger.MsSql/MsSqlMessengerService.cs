using System;
using System.Collections.Generic;
using System.Linq;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.MsSql.Orm;
using FatQueue.Messenger.MsSql.Server;
using JetBrains.Annotations;

namespace FatQueue.Messenger.MsSql
{
    [UsedImplicitly]
    public class MsSqlMessengerService : IMessengerService
    {
        private readonly ILogger _logger;
        private readonly MsSqlRepository _repository;

        public MsSqlMessengerService(string connectionString, ILogger logger)
        {
            _logger = logger;
            _repository = new MsSqlRepository(connectionString);
        }

        public IEnumerable<QueueStatus> GetQueueStatuses()
        {
            return _repository.GetQueueStatuses();
        }

        public IEnumerable<MessengerStatus> GetMessengerStatus()
        {
            return _repository.GetMessengerStatus();
        }

        public IEnumerable<ProcessStatus> GetActiveProcesses()
        {
            return _repository.GetActiveProcesses();
        }

        public IEnumerable<CompletedMessageDetails> GetCompletedMessages(int pageNo, int pageSize, DateTime? @from, DateTime? to)
        {
            return _repository.GetCompletedMessages(pageNo, pageSize, from, to);
        }

        public IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize, DateTime? from, DateTime? to)
        {
            return _repository.GetMessages(queueId, pageNo, pageSize, from, to);
        } 

        public IEnumerable<FailedMessageDetails> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to)
        {
            return _repository.GetFailedMessages(pageNo, pageSize, from, to);
        }

        public MessageDetails GetMessage(int messageId)
        {
            return _repository.GetMessage(messageId);
        }

        public CompletedMessageDetails GetCompletedMessage(int messageId)
        {
            return _repository.GetCompletedMessage(messageId);
        }

        public FailedMessageDetails GetFailedMessage(int messageId)
        {
            return _repository.GetFailedMessage(messageId);
        }

        public void ReenqueueFailedMessages(int[] ids, string queueName = null)
        {
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(queueName))
            {
                queueName = "FatQueue.Messenger.Failed";
            }

            int queueId = MsSqlMessenger.GetQueueId(queueName, _repository, _logger);
            _repository.ReenqueueFailedMessages(queueId, ids);
        }

        public void RecoverFailedMessages()
        {
            const int pageSize = ServerSettings.FailedMessageBatchSize;
            const int fromInMinutes = ServerSettings.FailedMessageRecoveryFromMinutes;    // if tasks are failing for more than 5 days - give up
            const int toInMinutes = ServerSettings.FailedMessageRecoveryToMinutes;    

            try
            {
                var from = DateTime.UtcNow.AddMinutes(-fromInMinutes);
                var to = DateTime.UtcNow.AddMinutes(-toInMinutes);
                bool finished = false;
                int pageNo = 0;

                while (!finished)
                {
                    pageNo += 1;

                    var failedMessages = GetFailedMessages(pageNo, pageSize, from, to);
                    if (failedMessages != null)
                    {
                        var ids = failedMessages.Select(message => message.FailedMessageId).ToArray();
                        if (ids.Length > 0)
                        {
                            ReenqueueFailedMessages(ids);
                        }

                        finished = ids.Length < pageSize;
                    }
                    else
                    {
                        finished = true;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.GetFormattedError("Failed to recover failed messages"));
            }
        }

        public void ReleaseProcessLock(string processName)
        {
            try
            {
                _repository.ReleaseProcessLock(processName);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetFormattedError("Release process lock failed"));
            }
        }
    }
}
