using System;
using System.Linq;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Tools;
using FatQueue.Messenger.PostgreSql;
using NUnit.Framework;
using StackExchange.Profiling;

namespace FatQueue.Messenger.Tests.IntegrationTests
{
    [TestFixture]
    public class PostgreSqlRepositoryShould
    {
        private readonly SqlSettings _sqlSettings = new SqlSettings
        {
            ConnectionString = "Server=127.0.0.1;Database=Messenger;User Id=admin;Password=admin;Enlist=true"
        };

        private PostgreSqlRepositoryFactory Factory
        {
            get
            {
                return new PostgreSqlRepositoryFactory(() => new PostgreSqlRepository(_sqlSettings));
            }
        }

        private IRepository _repository;
        private int _queueId;
        private Guid _identity;
        private string _queueName;
        private string _processName;

        [SetUp]
        public void Setup()
        {
            MiniProfiler.Start();

            _identity = Guid.NewGuid();
            _queueName = Guid.NewGuid().ToString("N");
            _processName = Guid.NewGuid().ToString("N");

            _repository = Factory.Create();

            _repository.ReleaseProcessLock(_processName);

            _queueId = _repository.CreateQueue(_queueName);

            var message = new SampleAction {Name = Guid.NewGuid().ToString()};
            _repository.CreateMessage(_queueId, "Test", JsonSerializer.ToJson(message), null, null, 0, Guid.NewGuid());
            _repository.CreateMessage(_queueId, "Test", JsonSerializer.ToJson(message), null, null, 0, _identity);
        }

        [TearDown]
        public void Cleanup()
        {
            var queueStatus = _repository.GetQueueStatuses();
            foreach (var queue in queueStatus)
            {
                var messages = _repository.GetMessages(queue.QueueId, 1, 10);
                foreach (var details in messages)
                {
                    _repository.RemoveMessage(new [] {details.MessageId});
                }

                _repository.DeleteQueue(queue.QueueId);
            }
        }

        [Test]
        public void FetchNonExistingQueueId()
        {
            var id = _repository.FetchQueueId(Guid.NewGuid().ToString("N"));
            Assert.IsNull(id);
        }

        [Test]
        public void FetchExistingQueueId()
        {
            var id = _repository.FetchQueueId(_queueName);
            Assert.IsNotNull(id);
            Assert.AreEqual(_queueId, id);
        }

        [Test]
        public void LockQueue()
        {
            var queueStatus = _repository.GetQueueStatuses();
            var lockedQueue = _repository.LockQueue(_processName);

            Assert.IsNotNull(lockedQueue);

            var lockedQueueStatus = queueStatus.FirstOrDefault(q => q.QueueId == lockedQueue.QueueId);
            Assert.IsNotNull(lockedQueueStatus);

            var messages = _repository.FetchQueueMessages(lockedQueue.QueueId, 1);
            Assert.IsNotNull(messages);
            Assert.IsNotEmpty(messages);

            _repository.ReleaseQueue(lockedQueueStatus.QueueId);
        }

        [Test]
        public void Hearbeat()
        {
            var repository = Factory.Create();
            repository.Heartbeat(_processName);
        }

        public class SampleAction
        {
            public string Name { get; set; }
        }
    }
}
