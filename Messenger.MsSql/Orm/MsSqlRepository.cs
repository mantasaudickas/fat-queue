using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Transactions;
using Dapper;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.MsSql.Data;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.MsSql.Orm
{
    internal class MsSqlRepository
    {
        private readonly string _connectionString;

        public MsSqlRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ITransaction CreateProcessingTransaction(TransactionIsolationLevel jobTransactionIsolationLevel, TimeSpan jobTimeout)
        {
            var isolationLevel = (IsolationLevel) jobTransactionIsolationLevel;
            return new Transaction(isolationLevel, jobTimeout, TransactionScopeOption.RequiresNew);
        }

        public int? FetchQueueId(string queueName)
        {
            const string sql = "SELECT [QueueId] FROM [Messenger].[Queues] (nolock) WHERE [Name] = @queueName";

            int? queueId;

            using (var db = new Database(_connectionString))
            {
                queueId = db.Connection.ExecuteScalar<int?>(sql, new { queueName });
                db.Complete();
            }

            return queueId;
        }

        public int CreateQueue(string queueName)
        {
            const string fetchSql = "SELECT [QueueId] FROM [Messenger].[Queues] WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE [Name] = @queueName";
            const string insertSql =
                "INSERT INTO [Messenger].[Queues] ([Name]) VALUES(@queueName); SELECT SCOPE_IDENTITY();";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                var queueId = db.Connection.ExecuteScalar<int?>(fetchSql, new { queueName });
                if (!queueId.HasValue)
                    queueId = db.Connection.ExecuteScalar<int>(insertSql, new { queueName });
                db.Complete();
                return queueId.Value;
            }
        }

        public void CreateMessage(int queueId, string contentType, string content, string context)
        {
            const string sql =
                "INSERT INTO [Messenger].[Messages] ([QueueId], [ContentType], [Content], [CreateDate], [Context]) " +
                "VALUES(@QueueId, @ContentType, @Content, SYSUTCDATETIME(), @Context)";

            var message = new Message
            {
                QueueId = queueId,
                ContentType = contentType,
                Content = content,
                Context = context
            };

            using (var db = new Database(_connectionString))
            {
                db.Connection.Execute(sql, message);
                db.Complete();
            }
        }

        public void InsertMessage(int queueId, string contentType, string content, string context)
        {
            const string selectSql =
                "SELECT [CreateDate] " +
                "FROM [Messenger].[Messages] " +
                "WHERE [QueueId] = @queueId " +
                "ORDER BY [CreateDate] ASC";

            const string sql =
                "INSERT INTO [Messenger].[Messages] ([QueueId], [ContentType], [Content], [CreateDate], [Context]) " +
                "VALUES(@QueueId, @ContentType, @Content, @CreateDate, @Context)";

            using (var db = new Database(_connectionString))
            {
                var createDate = db.Connection.ExecuteScalar<DateTime?>(selectSql, new {queueId});

                var parameters = new DynamicParameters();
                parameters.Add("QueueId", queueId);
                parameters.Add("ContentType", contentType);
                parameters.Add("Content", content);
                parameters.Add("CreateDate", createDate.GetValueOrDefault(DateTime.UtcNow), DbType.DateTime2);
                parameters.Add("Context", context);

                db.Connection.Execute(sql, parameters);
                db.Complete();
            }
        }

        public QueueInfo LockQueue(string processName)
        {
/*
            const string sql = "update top (1) Messenger.Queues with (rowlock) " +
                               "set ProcessingStarted = SYSUTCDATETIME() " +
                               ", ProcessName = @processName " +
                               "output inserted.Name, inserted.QueueId, inserted.Retries " +
                               "where QueueId = ( " +
                               "		select top 1 q.QueueId  " +
                               "		from Messenger.Queues q (readpast) " +
                               "		join Messenger.Messages m with (readpast) on m.QueueId = q.QueueId " +
                               "		where q.ProcessingStarted is null and (q.NextTryTime is null or q.NextTryTime < SYSUTCDATETIME()) " +
                               "		order by m.CreateDate asc)";
*/

            const string fetchSql =
                "select top 1 q.QueueId " +
                "from Messenger.Queues q with (updlock, readpast, rowlock) " +
                "join Messenger.Messages m (nolock) on m.QueueId = q.QueueId " +
                "where q.ProcessingStarted is null and (q.NextTryTime is null or q.NextTryTime < SYSUTCDATETIME()) " +
                "order by m.CreateDate asc";

            const string updateSql = "update Messenger.Queues " +
                                     "set ProcessingStarted = SYSUTCDATETIME() " +
                                     ", ProcessName = @processName " +
                                     ", ProcessedAt = SYSUTCDATETIME() " +
                                     "output inserted.Name, inserted.QueueId, inserted.Retries " +
                                     "where QueueId = @queueId";

            QueueInfo queueInfo = null;
            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                var queueId = db.Connection.Query<int?>(fetchSql).FirstOrDefault();
                if (queueId.HasValue)
                {
                    queueInfo = db.Connection.Query<QueueInfo>(updateSql, new {queueId, processName }).FirstOrDefault();
                }

                //var queue = db.Connection.Query<QueueInfo>(sql, new {processName}).FirstOrDefault();
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
                "where QueueId = @queueId";

            using (var db = new Database(_connectionString, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new {queueId});
                db.Complete();
            }
        }

        public void SetQueueFailure(int queueId, int retries, string formattedError, DateTime nextTryTime)
        {
            const string sql = "update [Messenger].[Queues] " +
                               "set ProcessingStarted = null " +
                               ", ProcessName = null " +
                               ", Error = @formattedError " +
                               ", Retries = @retries " +
                               ", NextTryTime = @nextTryTime " +
                               "where QueueId = @queueId";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { queueId, retries, formattedError, nextTryTime });
                db.Complete();
            }
        }

        public IList<MessageInfo> FetchQueueMessages(int queueId, int messageCount)
        {
            const string sql = "select top(@messageCount) MessageId, Content, Context " +
                               "from Messenger.Messages " +
                               "where QueueId = @queueId " +
                               "order by CreateDate asc, MessageId asc";

            using (var db = new Database(_connectionString, TransactionScopeOption.Suppress))
            {
                var messages = db.Connection.Query<MessageInfo>(sql, new { queueId, messageCount }).ToList();
                db.Complete();
                return messages;
            }
        }

        public void RemoveMessage(int messageId, bool archiveMessage)
        {
            const string sql = "delete from [Messenger].[Messages] where MessageId = @messageId";

            const string sqlArchive =
                "insert into [Messenger].[CompletedMessages] (ContentType, Content, CreateDate, Context, CompletedDate) " +
                "select ContentType, Content, CreateDate, Context, SYSUTCDATETIME() " +
                "from [Messenger].[Messages] " +
                "where MessageId = @messageId";

            using (var db = new Database(_connectionString, TransactionScopeOption.Required))
            {
                if (archiveMessage)
                {
                    db.Connection.Execute(sqlArchive, new {messageId});
                }

                db.Connection.Execute(sql, new {messageId});
                db.Complete();
            }
        }

        public void RemoveFirstQueueMessage(int messageId)
        {
            const string sql =
                "delete from Messenger.Messages " +
                "where MessageId = @messageId";

            using (var db = new Database(_connectionString, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void CopyMessageToFailed(int messageId)
        {
            const string sql =
                "insert into Messenger.FailedMessages (ContentType, Content, CreateDate, Error, FailedDate, Context) " +
                "select m.ContentType, m.Content, m.CreateDate, q.Error, SYSUTCDATETIME(), m.Context  " +
                "from Messenger.Messages m " +
                "join Messenger.Queues q on q.QueueId = m.QueueId " +
                "where m.MessageId = @messageId ";

            using (var db = new Database(_connectionString, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }            
        }

        public void MoveMessageToEnd(int messageId)
        {
            const string sql = "update Messenger.Messages " +
                               "set CreateDate = SYSUTCDATETIME() " +
                               "where MessageId = @messageId";

            using (var db = new Database(_connectionString, TransactionScopeOption.Required))
            {
                db.Connection.Execute(sql, new { messageId });
                db.Complete();
            }
        }

        public void Heartbeat(string processName)
        {
            const string sql =
                //"MERGE [Messenger].[Heartbeat] WITH (HOLDLOCK) AS target " +
                "MERGE [Messenger].[Heartbeat] AS target " +
                "USING (SELECT @processName) AS source (ProcessName) " +
                "ON (target.ProcessName = source.ProcessName) " +
                "WHEN MATCHED THEN " +
                "   UPDATE SET LastBeat = SYSUTCDATETIME() " +
                "WHEN NOT MATCHED THEN " +
                "   INSERT (ProcessName, LastBeat) " +
                "   VALUES (source.ProcessName, SYSUTCDATETIME());";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new { processName });
                db.Complete();
            }
        }

        public void ClearStaleProcesses(DateTime olderThan)
        {
            const string sql = "select ProcessName from [Messenger].[Heartbeat] where LastBeat < @olderThan";
            const string sqlUpdate = "update [Messenger].[Queues] set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName in @processNames";
            const string sqlDelete = "delete from [Messenger].[Heartbeat] where ProcessName in @processNames";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                var processNames = db.Connection.Query<string>(sql, new { olderThan }).ToArray();
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

            const string sqlUpdate = "update [Messenger].[Queues] set ProcessName = null, ProcessingStarted = null, NextTryTime = null where ProcessName in @processNames";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sqlUpdate, new {processNames});
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

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
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

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var status = db.Connection.Query<MessengerStatus>(sql);
                db.Complete();
                return status;
            }
        }

        public IEnumerable<ProcessStatus> GetActiveProcesses()
        {
            const string sql = "select ProcessName, LastBeat as LastHeartbeat from Messenger.Heartbeat";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<ProcessStatus>(sql);
                db.Complete();
                return statuses;
            }
        }

        public IEnumerable<FailedMessage> GetFailedMessages(int pageNo, int pageSize, DateTime? from, DateTime? to)
        {
            const string sql = "select *  " +
                               "from Messenger.FailedMessages " +
                               "where CreateDate between @timeFrom and @timeTo " +
                               "order by CreateDate asc " +
                               "offset @offset rows " +
                               "fetch next @pageSize rows only";

            int offset = (pageNo - 1)*pageSize;

            var timeFrom = from.GetValueOrDefault(SqlDateTime.MinValue.Value);
            var timeTo = to.GetValueOrDefault(DateTime.UtcNow);

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew, IsolationLevel.ReadUncommitted))
            {
                var statuses = db.Connection.Query<FailedMessage>(sql, new {offset, pageSize, timeFrom, timeTo});
                db.Complete();
                return statuses;
            }
        }

        public void PurgeCompletedMessages(DateTime olderThan)
        {
            const string sql = "delete from Messenger.CompletedMessages where CompletedDate < @olderThan";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
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

                if (filter.Count >= 100 || i+1 == ids.Length)
                {
                    ReenqueueFailedMessages(queueId, filter);
                }
            }
        }

        private void ReenqueueFailedMessages(int queueId, List<int> filter)
        {
            const string sql = "insert into Messenger.Messages (QueueId, ContentType, Content, CreateDate, Context) " +
                               "select @queueId, ContentType, Content, SYSUTCDATETIME(), Context " +
                               "from Messenger.FailedMessages " +
                               "where FailedMessageId in @id";

            const string sqlDelete = "delete from Messenger.FailedMessages where FailedMessageId in @id";

            using (var db = new Database(_connectionString, TransactionScopeOption.RequiresNew))
            {
                db.Connection.Execute(sql, new {queueId, id = filter.ToArray()});
                db.Connection.Execute(sqlDelete, new { queueId, id = filter.ToArray() });
                db.Complete();
            }
        }
    }
}
