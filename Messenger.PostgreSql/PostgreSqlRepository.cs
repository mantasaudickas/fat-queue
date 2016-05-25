using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Transactions;
using Dapper;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Data;
using FatQueue.Messenger.Core.Database;
using FatQueue.Messenger.Core.Orm;
using Npgsql;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlRepository : IRepository
    {
        private readonly Func<IDbConnection> _connectionFactory;

        public PostgreSqlRepository(SqlSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _connectionFactory = () => new NpgsqlConnection(settings.ConnectionString);
        }

        public PostgreSqlRepository(Func<IDbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public ITransaction CreateProcessingTransaction(TransactionIsolationLevel jobTransactionIsolationLevel, TimeSpan jobTimeout)
        {
            var isolationLevel = (IsolationLevel)jobTransactionIsolationLevel;
            return new FatQueueTransaction(isolationLevel, jobTimeout, _connectionFactory, TransactionScopeOption.RequiresNew);
        }

        public int? FetchQueueId(string queueName)
        {
            const string sql = "SELECT QueueId FROM Messenger.Queues WHERE Name = :queueName";

            int? queueId;

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                queueId = db.Connection.ExecuteScalar<int?>(sql, new {queueName});
                db.Complete();
            }

            return queueId;
        }

        public int CreateQueue(string queueName)
        {
            const string fetchSql = "SELECT QueueId FROM Messenger.Queues WHERE Name = :queueName";
            const string insertSql = "INSERT INTO Messenger.Queues (Name) VALUES(:queueName); SELECT LASTVAL();";
            const string lockSql = "LOCK TABLE Messenger.Queues";

            int? queueId;
            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                queueId = db.Connection.ExecuteScalar<int?>(fetchSql, new {queueName});
                if (!queueId.HasValue)
                {
                    db.Connection.Execute(lockSql);
                    queueId = db.Connection.ExecuteScalar<int?>(fetchSql, new {queueName});
                    if (!queueId.HasValue)
                    {
                        queueId = db.Connection.ExecuteScalar<int>(insertSql, new {queueName});
                    }
                }
                db.Complete();
            }
            return queueId.Value;
        }

        public void DeleteQueue(int queueId)
        {
            const string sql = "DELETE FROM Messenger.Queues WHERE QueueId = :queueId";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new {queueId});
                db.Complete();
            }
        }

        public void CreateMessage(int queueId, string contentType, string content, string context, string contextFactory, int delayInSeconds, Guid identity)
        {
            const string sql =
                "INSERT INTO Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity) " +
                "VALUES(:QueueId, :ContentType, :Content, CURRENT_TIMESTAMP + INTERVAL '{0} second', :Context, :Identity, :ContextFactory)";

            var message = new
            {
                QueueId = queueId,
                ContentType = contentType,
                Content = content,
                DelayInSeconds = delayInSeconds,
                Context = context,
                Identity = identity,
                ContextFactory = contextFactory
            };

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(string.Format(sql, delayInSeconds), message);
                db.Complete();
            }
        }

        public void CreateMessageIfNotExists(int queueId, string contentType, string content, string contextFactory, string context,
            int delayInSeconds, Guid identity)
        {
            const string sql =
                "INSERT INTO Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity) " +
                "VALUES(:QueueId, :ContentType, :Content, CURRENT_TIMESTAMP + INTERVAL '{0} second', :Context, :Identity, :ContextFactory)";

            var message = new
            {
                QueueId = queueId,
                ContentType = contentType,
                Content = content,
                DelayInSeconds = delayInSeconds,
                Context = context,
                Identity = identity,
                ContextFactory = contextFactory
            };

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(string.Format(sql, delayInSeconds), message);
                db.Complete();
            }
        }

        public void InsertMessage(int queueId, string contentType, string content, string context, string contextFactory, Guid identity)
        {
            const string selectSql =
                "SELECT StartDate " +
                "FROM Messenger.Messages " +
                "WHERE QueueId = :queueId " +
                "ORDER BY StartDate ASC";

            const string sql =
                "INSERT INTO Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, Identity, ContextFactory) " +
                "VALUES(:QueueId, :ContentType, :Content, :StartDate, :Context, :Identity, :ContextFactory)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                var createDate = db.Connection.ExecuteScalar<DateTime?>(selectSql, new { queueId });

                var parameters = new DynamicParameters();
                parameters.Add("QueueId", queueId);
                parameters.Add("ContentType", contentType);
                parameters.Add("Content", content);
                parameters.Add("StartDate", createDate.GetValueOrDefault(DateTime.UtcNow));
                parameters.Add("Context", context);
                parameters.Add("Identity", identity);
                parameters.Add("ContextFactory", contextFactory);

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

            QueueInfo queueInfo;
            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                queueInfo = db.Connection
                    .Query<QueueInfo>(updateSql, new {processName})
                    .FirstOrDefault();
                db.Complete();
            }

            return queueInfo;
        }

        public void ReleaseQueue(int queueId, ITransaction transaction = null)
        {
            const string sql =
                "update Messenger.Queues " +
                "set ProcessingStarted = null " +
                ", ProcessName = null " +
                ", Error = null " +
                ", Retries = null " +
                ", NextTryTime = null " +
                "where QueueId = :queueId";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new {queueId});
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

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new {queueId, retries, formattedError, nextTryTime});
                db.Complete();
            }
        }

        public IList<MessageInfo> FetchQueueMessages(int queueId, int messageCount)
        {
            const string sql = "select MessageId, Content, Context, StartDate, Identity, ContextFactory " +
                               "from Messenger.Messages " +
                               "where QueueId = :queueId and StartDate <= CURRENT_TIMESTAMP " +
                               "order by StartDate asc " +
                               "limit (:messageCount)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.Unspecified))
            {
                var messages = db.Connection.Query<MessageInfo>(sql, new { queueId, messageCount }).ToList();
                db.Complete();
                return messages;
            }
        }

        public int CancelMessages(Guid identity)
        {
            const string sql = "delete from Messenger.Messages where Identity = :identity and StartDate > CURRENT_TIMESTAMP";

            int affectedRows;
            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                affectedRows = db.Connection.Execute(sql, new {identity});
                db.Complete();
            }
            return affectedRows;
        }

        public void RemoveMessage(int messageId, bool archiveMessage, ITransaction transaction = null)
        {
            const string sql = "delete from Messenger.Messages where MessageId = :messageId";

            const string sqlArchive =
                "insert into Messenger.CompletedMessages (ContentType, Content, CreateDate, Context, CompletedDate, Identity, ContextFactory) " +
                "select ContentType, Content, StartDate, Context, CURRENT_TIMESTAMP, Identity, ContextFactory " +
                "from Messenger.Messages " +
                "where MessageId = :messageId";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                if (archiveMessage)
                {
                    db.Connection.Execute(sqlArchive, new { messageId });
                }

                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void RemoveMessage(int [] messageId, ITransaction transaction = null)
        {
            if (messageId == null || messageId.Length == 0)
                return;

            const string sql =
                "delete from Messenger.Messages " +
                "where MessageId = ANY(:messageId)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void CopyMessageToFailed(int messageId, ITransaction transaction = null)
        {
            const string sql =
                "insert into Messenger.FailedMessages (ContentType, Content, CreateDate, Error, FailedDate, Context, Identity, ContextFactory) " +
                "select m.ContentType, m.Content, m.StartDate, q.Error, CURRENT_TIMESTAMP, m.Context, m.Identity, m.ContextFactory  " +
                "from Messenger.Messages m " +
                "join Messenger.Queues q on q.QueueId = m.QueueId " +
                "where m.MessageId = :messageId ";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void MoveMessageToEnd(int messageId, ITransaction transaction = null)
        {
            const string sql = "update Messenger.Messages " +
                               "set StartDate = CURRENT_TIMESTAMP " +
                               "where MessageId = :messageId";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.Required))
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

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new {processName});
                db.Complete();
            }
        }

        public void ClearStaleProcesses(DateTime olderThan)
        {
            const string sql = "select ProcessName from Messenger.Heartbeat where LastBeat < :olderThan";
            const string sqlUpdate = "update Messenger.Queues set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName = ANY(:processNames)";
            const string sqlDelete = "delete from Messenger.Heartbeat where ProcessName = ANY(:processNames)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                var processNames = db.Connection.Query<string>(sql, new {olderThan}).ToArray();
                if (processNames.Length > 0)
                {
                    db.Connection.Execute(sqlUpdate, new {processNames});
                    db.Connection.Execute(sqlDelete, new {processNames});
                }
                db.Complete();
            }
        }

        public void ReleaseProcessLock(params string [] processNames)
        {
            if (processNames == null || processNames.Length == 0)
                return;

            const string sqlUpdate = "update Messenger.Queues set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName = ANY(:processNames)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sqlUpdate, new {processNames});
                db.Complete();
            }
        }

        public void PurgeCompletedMessages(DateTime olderThan)
        {
            const string sql = "delete from Messenger.CompletedMessages where CompletedDate < :olderThan";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new {olderThan});
                db.Complete();
            }
        }

        // -------------------- QUEUE MANAGEMENT ---------------------

        public IEnumerable<MessengerStatus> GetMessengerStatus()
        {
            const string sql = "select 'Failed' as Status, count(*) as MessageCount from Messenger.FailedMessages " +
                               "union " +
                               "select 'Completed', count(*) from Messenger.CompletedMessages " +
                               "union " +
                               "select 'Ready', count(*) from Messenger.Messages";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var status = db.Connection.Query<MessengerStatus>(sql);
                db.Complete();
                return status;
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

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<QueueStatus>(sql);
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<ProcessStatus> GetActiveProcesses()
        {
            const string sql = "select ProcessName, LastBeat as LastHeartbeat from Messenger.Heartbeat";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<ProcessStatus>(sql);
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<MessageDetails> GetMessages(int queueId, int pageNo, int pageSize)
        {
            const string sql = "select *  " +
                               "from Messenger.Messages " +
                               "where QueueId = :queueId " +
                               "order by StartDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            IEnumerable<MessageDetails> statuses;
            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                statuses = db.Connection.Query<MessageDetails>(sql, new {queueId, offset, pageSize});
                db.Complete();
            }
            return statuses;
        }

        public IEnumerable<MessageDetails> GetCompletedMessages(int pageNo, int pageSize)
        {
            const string sql = "select *  " +
                               "from Messenger.CompletedMessages " +
                               "--where CreateDate between :timeFrom and :timeTo " +
                               "order by CreateDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<MessageDetails>(sql, new { offset, pageSize });
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<MessageDetails> GetFailedMessages(int pageNo, int pageSize)
        {
            const string sql = "select *  " +
                               "from Messenger.FailedMessages " +
                               "order by CreateDate asc " +
                               "limit :pageSize " +
                               "offset :offset";

            int offset = (pageNo - 1) * pageSize;

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<MessageDetails>(sql, new { offset, pageSize });
                db.Complete();
                return statuses;
            }
        }

        public MessageDetails GetMessageDetails(Guid identity)
        {
            const string sql = "select *  " +
                               "from Messenger.Messages " +
                               "where Identity = :identity ";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<MessageDetails>(sql, new { identity });
                db.Complete();
                return statuses.FirstOrDefault();
            }
        }

        public void RemoveMessages(params Guid[] identity)
        {
            if (identity == null || identity.Length == 0)
                return;

            const string readySql = "delete from Messenger.Messages where Identity = ANY(:identity)";
            const string failedSql = "delete from Messenger.FailedMessages where Identity = ANY(:identity)";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(readySql, new { identity });
                db.Connection.Execute(failedSql, new { identity });
                db.Complete();
            }
        }

        public void ReenqueueFailedMessages(int queueId, params Guid[] identity)
        {
            const string sql = "insert into Messenger.Messages (QueueId, ContentType, Content, StartDate, Context, [Identity], ContextFactory) " +
                               "select @queueId, ContentType, Content, SYSUTCDATETIME(), Context, [Identity], ContextFactory " +
                               "from Messenger.FailedMessages " +
                               "where [Identity] in @identity";

            const string sqlDelete = "delete from Messenger.FailedMessages where [Identity] in @identity";

            using (var db = new FatQueueDatabase(_connectionFactory, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { queueId, identity });
                db.Connection.Execute(sqlDelete, new { queueId, identity });
                db.Complete();
            }
        }
    }
}
