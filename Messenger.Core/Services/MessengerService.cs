using System;
using System.Collections.Generic;
using System.Linq;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Database;

namespace FatQueue.Messenger.Core.Services
{
    public class MessengerService : IMessengerService
    {
        private readonly ILogger _logger;
        private readonly IRepository _repository;

        public MessengerService(ILogger logger, RepositoryFactory factory)
        {
            _logger = logger;
            _repository = factory.Create();
        }

        public IEnumerable<MessengerStatus> GetMessengerStatus()
        {
            return _repository.GetMessengerStatus();
        }

        public IEnumerable<QueueStatus> GetQueueStatuses()
        {
            return _repository.GetQueueStatuses();
        }

        public IEnumerable<ProcessStatus> GetActiveProcesses()
        {
            return _repository.GetActiveProcesses();
        }

        public IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize)
        {
            return _repository.GetMessages(queueId, pageNo, pageSize);
        }

        public IEnumerable<MessageDetails> GetCompletedMessages(int pageNo, int pageSize)
        {
            return _repository.GetCompletedMessages(pageNo, pageSize);
        }

        public IEnumerable<MessageDetails> GetFailedMessages(int pageNo, int pageSize)
        {
            return _repository.GetFailedMessages(pageNo, pageSize);
        }

        public MessageDetails GetMessageDetails(Guid identity)
        {
            return _repository.GetMessageDetails(identity);
        }

        public void RemoveMessages(params Guid[] identity)
        {
            _repository.RemoveMessages(identity);
        }

        public int ReenqueueFailedMessages()
        {
            const int pageSize = ServerSettings.FailedMessageBatchSize;
            //const int fromInMinutes = ServerSettings.FailedMessageRecoveryFromMinutes;    // if tasks are failing for more than 5 days - give up
            //const int toInMinutes = ServerSettings.FailedMessageRecoveryToMinutes;    

            int? queueId = null;
            try
            {
                //var from = DateTime.UtcNow.AddMinutes(-fromInMinutes);
                //var to = DateTime.UtcNow.AddMinutes(-toInMinutes);
                bool finished = false;
                int pageNo = 0;

                while (!finished)
                {
                    pageNo += 1;

                    var failedMessages = GetFailedMessages(pageNo, pageSize);
                    if (failedMessages != null)
                    {
                        var ids = failedMessages.Select(message => message.Identity).ToArray();
                        if (ids.Length > 0)
                        {
                            queueId = ReenqueueFailedMessages(queueId, null, ids);
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

            return queueId.GetValueOrDefault();
        }

        public int ReenqueueFailedMessages(string queueName, params Guid[] identity)
        {
            return ReenqueueFailedMessages(null, queueName, identity);
        }

        private int ReenqueueFailedMessages(int? queueId, string queueName, Guid[] identity)
        {
            if (identity == null || identity.Length == 0)
            {
                return 0;
            }

            if (!queueId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(queueName))
                    queueName = "Messenger.Retry.Failed";

                queueId = Messenger.GetQueueId(queueName, _repository, _logger);
            }

            _repository.ReenqueueFailedMessages(queueId.Value, identity);

            return queueId.Value;
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
