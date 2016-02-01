using System;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.MsSql.Orm
{
    internal class Database : IDisposable
    {
        private readonly string _connectionString;
        private readonly TransactionScope _transactionScope;
        private IDbConnection _connection;

        public Database(
            string connectionString, 
            TransactionScopeOption? transactionType = null, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
        {
            _connectionString = connectionString;

            if (!transactionType.HasValue)
                return;

            var transactionTypeValue = transactionType.Value;

            if (System.Transactions.Transaction.Current != null && transactionTypeValue == TransactionScopeOption.Required)
                return;

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel
            };

            _transactionScope = new TransactionScope(transactionTypeValue, transactionOptions);
        }

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = new SqlConnection(_connectionString);
                    _connection.Open();
                }
                return _connection;
            }
        }

        public void Complete()
        {
            _transactionScope?.Complete();
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _transactionScope?.Dispose();

            _connection = null;
        }
    }
}
