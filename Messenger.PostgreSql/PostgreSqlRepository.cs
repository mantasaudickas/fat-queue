using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Dapper;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Data;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Orm;
using Npgsql;
using IsolationLevel = System.Transactions.IsolationLevel;
using Transaction = FatQueue.Messenger.Core.Orm.Transaction;

namespace FatQueue.Messenger.PostgreSql
{
    internal class PostgreSqlRepository : IRepository
    {
        private readonly Func<IDbConnection> _connectionFactory;

        public PostgreSqlRepository(SqlSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _connectionFactory = () => new NpgsqlConnection(settings.ConnectionString);
        }

        public ITransaction CreateProcessingTransaction(TransactionIsolationLevel jobTransactionIsolationLevel, TimeSpan jobTimeout)
        {
            var isolationLevel = (IsolationLevel)jobTransactionIsolationLevel;
            return new Transaction(isolationLevel, jobTimeout, TransactionScopeOption.RequiresNew);
        }

        public int? FetchQueueId(string queueName)
        {
            const string sql = "SELECT QueueId FROM Messenger.Queues WHERE Name = :queueName";

            int? queueId;

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                queueId = db.Connection.ExecuteScalar<int?>(sql, new { queueName });
                db.Complete();
            }

            return queueId;
        }

        public int CreateQueue(string queueName)
        {
            const string fetchSql = "SELECT QueueId FROM Messenger.Queues WHERE Name = :queueName";
            const string lockSql = "LOCK TABLE Messenger.Queues";
            const string insertSql = "INSERT INTO Messenger.Queues (Name) VALUES(:queueName); SELECT LASTVAL();";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                var queueId = db.Connection.ExecuteScalar<int?>(fetchSql, new { queueName });
                if (!queueId.HasValue)
                {
                    db.Connection.Execute(lockSql);
                    queueId = db.Connection.ExecuteScalar<int?>(fetchSql, new { queueName });
                    if (!queueId.HasValue)
                        queueId = db.Connection.ExecuteScalar<int>(insertSql, new { queueName });
                }
                db.Complete();
                return queueId.Value;
            }
        }

        public void DeleteQueue(int queueId)
        {
            const string sql = "DELETE FROM Messenger.Queues WHERE QueueId = :queueId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { queueId });
                db.Complete();
            }
        }

        public void CreateMessage(int queueId, string contentType, string content, string context, int delayInSeconds, Guid? identity)
        {
            const string sql =
                "INSERT INTO Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity) " +
                "VALUES(:QueueId, :ContentType, :Content, CURRENT_TIMESTAMP + INTERVAL '{0} second', :Context, :Identity)";

            var message = new
            {
                QueueId = queueId,
                ContentType = contentType,
                Content = content,
                DelayInSeconds = delayInSeconds,
                Context = context,
                Identity = identity,
            };

            using (var db = new Database(_connectionFactory))
            {
                db.Connection.Execute(string.Format(sql, delayInSeconds), message);
                db.Complete();
            }
        }

        public void InsertMessage(int queueId, string contentType, string content, string context, Guid? identity)
        {
            const string selectSql =
                "SELECT StartDate " +
                "FROM Messenger.Messages " +
                "WHERE QueueId = :queueId " +
                "ORDER BY StartDate ASC";

            const string sql =
                "INSERT INTO Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity) " +
                "VALUES(:QueueId, :ContentType, :Content, :StartDate, :Context, :Identity)";

            using (var db = new Database(_connectionFactory))
            {
                var createDate = db.Connection.ExecuteScalar<DateTime?>(selectSql, new { queueId });

                var parameters = new DynamicParameters();
                parameters.Add("QueueId", queueId);
                parameters.Add("ContentType", contentType);
                parameters.Add("Content", content);
                parameters.Add("StartDate", createDate.GetValueOrDefault(DateTime.UtcNow), DbType.DateTime2);
                parameters.Add("Context", context);
                parameters.Add("Identity", identity);

                db.Connection.Execute(sql, parameters);
                db.Complete();
            }
        }

        public QueueInfo LockQueue(string processName)
        {
            const string updateSql = 
                "update Messenger.Queues " +
                "set ProcessingStarted = CURRENT_TIMESTAMP" +
                ", ProcessName = :processName" +
                ", ProcessedAt = CURRENT_TIMESTAMP " +
                "where QueueId = (" +
                "   select q.QueueId " +
                "   from Messenger.Queues q " +
                "   join Messenger.Messages m on m.QueueId = q.QueueId " +
                "   where q.ProcessingStarted is null " +
                "   and m.StartDate <= CURRENT_TIMESTAMP " +
                "   and (q.NextTryTime is null or q.NextTryTime < CURRENT_TIMESTAMP) " +
                "   order by m.StartDate asc " +
                "   limit 1 " +
                "   for update skip locked) " +
                "returning QueueId, Name, Retries";

            QueueInfo queueInfo = null;
            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                queueInfo = db.Connection.Query<QueueInfo>(updateSql, new { processName }).FirstOrDefault();
                db.Complete();
            }

            return queueInfo;
        }

        public void ReleaseQueue(int queueId)
        {
            const string sql =
                "update Messenger.Queues " +
                "set ProcessingStarted = null " +
                ", ProcessName = null " +
                ", Error = null " +
                ", Retries = null " +
                ", NextTryTime = null " +
                "where QueueId = :queueId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { queueId });
                db.Complete();
            }
        }

        public void SetQueueFailure(int queueId, int retries, string formattedError, DateTime nextTryTime)
        {
            const string sql = "update Messenger.Queues " +
                               "set ProcessingStarted = null " +
                               ", ProcessName = null " +
                               ", Error = :formattedError " +
                               ", Retries = :retries " +
                               ", NextTryTime = :nextTryTime " +
                               "where QueueId = :queueId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { queueId, retries, formattedError, nextTryTime });
                db.Complete();
            }
        }

        public IList<MessageInfo> FetchQueueMessages(int queueId, int messageCount)
        {
            const string sql = "select MessageId, Content, Context, StartDate, Identity " +
                               "from Messenger.Messages " +
                               "where QueueId = :queueId and StartDate <= CURRENT_TIMESTAMP " +
                               "order by StartDate asc, MessageId asc " +
                               "limit (:messageCount)";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Suppress))
            {
                var messages = db.Connection.Query<MessageInfo>(sql, new { queueId, messageCount }).ToList();
                db.Complete();
                return messages;
            }
        }

        public int CancelMessages(Guid identity)
        {
            const string sql = "delete from Messenger.Messages where Identity = :identity and StartDate > CURRENT_TIMESTAMP";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.Serializable))
            {
                var affectedRows = db.Connection.Execute(sql, new { identity });
                db.Complete();
                return affectedRows;
            }
        }

        public void RemoveMessage(int messageId, bool archiveMessage)
        {
            const string sql = "delete from Messenger.Messages where MessageId = :messageId";

            const string sqlArchive =
                "insert into Messenger.CompletedMessages (ContentType, Content, CreateDate, Context, CompletedDate, Identity) " +
                "select ContentType, Content, StartDate, Context, CURRENT_TIMESTAMP, Identity " +
                "from Messenger.Messages " +
                "where MessageId = :messageId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Required))
            {
                if (archiveMessage)
                {
                    db.Connection.Execute(sqlArchive, new { messageId });
                }

                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void RemoveMessage(int messageId)
        {
            const string sql =
                "delete from Messenger.Messages " +
                "where MessageId = :messageId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void CopyMessageToFailed(int messageId)
        {
            const string sql =
                "insert into Messenger.FailedMessages (ContentType, Content, CreateDate, Error, FailedDate, Context, Identity) " +
                "select m.ContentType, m.Content, m.StartDate, q.Error, CURRENT_TIMESTAMP, m.Context, m.Identity  " +
                "from Messenger.Messages m " +
                "join Messenger.Queues q on q.QueueId = m.QueueId " +
                "where m.MessageId = :messageId ";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void MoveMessageToEnd(int messageId)
        {
            const string sql = "update Messenger.Messages " +
                               "set StartDate = CURRENT_TIMESTAMP " +
                               "where MessageId = :messageId";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void Heartbeat(string processName)
        {
            const string sql =
                "insert into Messenger.Heartbeat (ProcessName, Lastbeat) " +
                "values (:processName, CURRENT_TIMESTAMP) " +
                "on conflict (ProcessName) do update set Lastbeat = CURRENT_TIMESTAMP";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { processName });
                db.Complete();
            }
        }

        public void ClearStaleProcesses(DateTime olderThan)
        {
            const string sql = "select ProcessName from Messenger.Heartbeat where LastBeat < :olderThan";
            const string sqlUpdate = "update Messenger.Queues set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName in :processNames";
            const string sqlDelete = "delete from Messenger.Heartbeat where ProcessName in :processNames";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                var processNames = db.Connection.Query<string>(sql, new { olderThan }).ToArray();
                if (processNames.Length > 0)
                {
                    db.Connection.Execute(sqlUpdate, new { processNames });
                    db.Connection.Execute(sqlDelete, new { processNames });
                }
                db.Complete();
            }
        }

        public void ReleaseProcessLock(params string [] processNames)
        {
            if (processNames == null || processNames.Length == 0)
                return;

            const string sqlUpdate = "update Messenger.Queues set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName in :processNames";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sqlUpdate, new { processNames });
                db.Complete();
            }
        }

        public IEnumerable<QueueStatus> GetQueueStatuses()
        {
            const string sql =
                "select q.QueueId, q.Name, q.ProcessedAt, q.ProcessingStarted, q.ProcessName, q.Retries, q.NextTryTime, q.Error " +
                ", sum(case when m.MessageId is null then 0 else 1 end) as MessageCount " +
                "from Messenger.Queues q " +
                "left join Messenger.Messages m on m.QueueId = q.QueueId " +
                "group by q.QueueId, q.Name, q.ProcessedAt, q.ProcessingStarted, q.ProcessName, q.Retries, q.NextTryTime, q.Error";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<QueueStatus>(sql);
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<MessengerStatus> GetMessengerStatus()
        {
            const string sql = "select 'Failed' as Status, count(*) as MessageCount from Messenger.FailedMessages " +
                               "union " +
                               "select 'Completed', count(*) from Messenger.CompletedMessages " +
                               "union " +
                               "select 'Ready', count(*) from Messenger.Messages";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var status = db.Connection.Query<MessengerStatus>(sql);
                db.Complete();
                return status;
            }
        }

        public IEnumerable<ProcessStatus> GetActiveProcesses()
        {
            const string sql = "select ProcessName, LastBeat as LastHeartbeat from Messenger.Heartbeat";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<ProcessStatus>(sql);
                db.Complete();
                return statuses;
            }
        }

        public void PurgeCompletedMessages(DateTime olderThan)
        {
            const string sql = "delete from Messenger.CompletedMessages where CompletedDate < :olderThan";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { olderThan });
                db.Complete();
            }
        }

        public void ReenqueueFailedMessages(int queueId, int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return;

            var filter = new List<int>();
            for (int i = 0; i < ids.Length; ++i)
            {
                filter.Add(ids[i]);

                if (filter.Count >= 100 || i + 1 == ids.Length)
                {
                    ReenqueueFailedMessages(queueId, filter);
                }
            }
        }

        private void ReenqueueFailedMessages(int queueId, List<int> filter)
        {
            const string sql = "insert into Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity) " +
                               "select :queueId, ContentType, Content, CURRENT_TIMESTAMP, Context, Identity " +
                               "from Messenger.FailedMessages " +
                               "where FailedMessageId in :id";

            const string sqlDelete = "delete from Messenger.FailedMessages where FailedMessageId in :id";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { queueId, id = filter.ToArray() });
                db.Connection.Execute(sqlDelete, new { queueId, id = filter.ToArray() });
                db.Complete();
            }
        }

        public IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize, DateTime? @from, DateTime? to)
        {
            const string sql = "select *  " +
                               "from Messenger.Messages " +
                               "where QueueId = :queueId --and StartDate between :timeFrom and :timeTo " +
                               "order by StartDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            var timeFrom = from.GetValueOrDefault(SqlDateTime.MinValue.Value);
            var timeTo = to.GetValueOrDefault(DateTime.UtcNow);

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<MessageDetails>(sql, new { queueId, offset, pageSize, timeFrom, timeTo });
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<CompletedMessageDetails> GetCompletedMessages(int pageNo, int pageSize, DateTime? @from, DateTime? to)
        {
            const string sql = "select *  " +
                               "from Messenger.CompletedMessages " +
                               "--where CreateDate between :timeFrom and :timeTo " +
                               "order by CreateDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            var timeFrom = from.GetValueOrDefault(SqlDateTime.MinValue.Value);
            var timeTo = to.GetValueOrDefault(DateTime.UtcNow);

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<CompletedMessageDetails>(sql, new { offset, pageSize, timeFrom, timeTo });
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<FailedMessageDetails> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to)
        {
            const string sql = "select *  " +
                               "from Messenger.FailedMessages " +
                               "--where CreateDate between :timeFrom and :timeTo " +
                               "order by CreateDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            var timeFrom = from.GetValueOrDefault(SqlDateTime.MinValue.Value);
            var timeTo = to.GetValueOrDefault(DateTime.UtcNow);

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<FailedMessageDetails>(sql, new { offset, pageSize, timeFrom, timeTo });
                db.Complete();
                return statuses;
            }
        }

        public MessageDetails GetMessage(int messageId)
        {
            const string sql = "select *  " +
                               "from Messenger.Messages " +
                               "where MessageId = :messageId ";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<MessageDetails>(sql, new { messageId });
                db.Complete();
                return statuses.FirstOrDefault();
            }
        }

        public CompletedMessageDetails GetCompletedMessage(int messageId)
        {
            const string sql = "select *  " +
                               "from Messenger.CompletedMessages " +
                               "where CompletedMessageId = :messageId ";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<CompletedMessageDetails>(sql, new { messageId });
                db.Complete();
                return statuses.FirstOrDefault();
            }
        }

        public FailedMessageDetails GetFailedMessage(int messageId)
        {
            const string sql = "select *  " +
                               "from Messenger.FailedMessages " +
                               "where FailedMessageId = :messageId ";

            using (var db = new Database(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<FailedMessageDetails>(sql, new { messageId });
                db.Complete();
                return statuses.FirstOrDefault();
            }
        }
    }
}
