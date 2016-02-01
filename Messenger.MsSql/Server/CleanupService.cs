using System;
using System.Threading;
using System.Threading.Tasks;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.MsSql.Orm;

namespace FatQueue.Messenger.MsSql.Server
{
    internal class CleanupService
    {
        private readonly ILogger _logger;
        private readonly MsSqlRepository _repository;

        public CleanupService(string connectionString, ILogger logger)
        {
            _repository = new MsSqlRepository(connectionString);
            _logger = logger;
        }

        public void Start(Func<DateTime> cleanMessagesOlderThan, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => Purge(cleanMessagesOlderThan, cancellationToken), TaskCreationOptions.LongRunning);
        }

        private void Purge(Func<DateTime> olderThan, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Purge(olderThan());
                TimeSpan.FromMinutes(10).Sleep(_logger, cancellationToken);
            }
        }

        private void Purge(DateTime olderThan)
        {
            try
            {
                _repository.PurgeCompletedMessages(olderThan);
            }
            catch (Exception e)
            {
                _logger.Error(e.GetFormattedError("Failed to purge completed messages"));
            }
        }
    }
}
