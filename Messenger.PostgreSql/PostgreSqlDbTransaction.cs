using System;
using System.Data;
using System.Transactions;
using FatQueue.Messenger.Core.Orm;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.PostgreSql
{
    internal class PostgreSqlDbTransaction : FatQueueTransaction
    {
        private readonly IsolationLevel _isolationLevel;
        private IDbTransaction _transaction;

        public PostgreSqlDbTransaction(IsolationLevel isolationLevel, 
            TimeSpan? timeout, 
            Func<IDbConnection> connectionFactory, 
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, 
            TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Enabled) : 
            base(isolationLevel, timeout, connectionFactory, transactionScopeOption, transactionScopeAsyncFlowOption)
        {
            _isolationLevel = isolationLevel;
        }

        protected override IDbConnection CreateNewConnection()
        {
            var connection = base.CreateNewConnection();
            _transaction = connection.BeginTransaction(_isolationLevel.DataIsolationLevel());
            return connection;
        }

        public override void Complete()
        {
            _transaction.Commit();
            base.Complete();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
