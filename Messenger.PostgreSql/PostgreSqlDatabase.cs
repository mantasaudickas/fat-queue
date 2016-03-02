using System;
using System.Data;
using System.Transactions;
using FatQueue.Messenger.Core.Orm;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.PostgreSql
{
    public class PostgreSqlDatabase : ITransaction
    {
        private readonly IsolationLevel _isolationLevel;
        private readonly ITransaction _externalTransaction;
        private Func<IDbConnection> _connectionFactory;
        private IDbConnection _connection;
        private IDbTransaction _transaction;
        private bool _isCompleted;

        public PostgreSqlDatabase(
            Func<IDbConnection> connectionFactory,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            ITransaction externalTransaction = null)
        {
            _connectionFactory = connectionFactory;
            _isolationLevel = isolationLevel;
            _externalTransaction = externalTransaction;
        }

        public IDbConnection Connection
        {
            get
            {
                if (_externalTransaction != null)
                {
                    return _externalTransaction.Connection;
                }

                if (_connection == null)
                {
                    _connection = _connectionFactory();
                    _connection.Open();

                    if (_isolationLevel != IsolationLevel.Unspecified)
                    {
                        //_transaction = _connection.BeginTransaction(_isolationLevel.DataIsolationLevel());
                    }
                }
                return _connection;
            }
        }

        public IDbTransaction Transaction
        {
            get
            {
                if (_externalTransaction != null)
                {
                    return _externalTransaction.Transaction;
                }

                return _transaction;
            }
        }

        public void Complete()
        {
            _transaction?.Commit();
            _isCompleted = true;
        }

        public TransactionStatus? TransactionStatus
        {
            get
            {
                if (_transaction == null)
                    return null;
                return System.Transactions.TransactionStatus.Active;
            }
        }

        public void Dispose()
        {
            _connectionFactory = null;

            if (!_isCompleted)
                _transaction?.Rollback();

            _transaction?.Dispose();
            _connection?.Dispose();

            _connection = null;
        }
    }
}
