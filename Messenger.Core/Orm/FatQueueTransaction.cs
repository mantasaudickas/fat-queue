using System;
using System.Data;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.Core.Orm
{
    public class FatQueueTransaction : ITransaction
    {
        private IDbConnection _connection;
        private Func<IDbConnection> _connectionFactory;
        private CommittableTransaction _transaction;
        private TransactionScope _transactionScope;

        public FatQueueTransaction(
            IsolationLevel isolationLevel, 
            TimeSpan? timeout,
            Func<IDbConnection> connectionFactory,
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, 
            TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Enabled)
        {
            _connectionFactory = connectionFactory;

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = isolationLevel,
            };

            if (timeout != null)
            {
                transactionOptions.Timeout = timeout.Value;
            }

            if (System.Transactions.Transaction.Current == null || transactionScopeOption == TransactionScopeOption.RequiresNew)
            {
                _transaction = new CommittableTransaction(transactionOptions);
                _transactionScope = new TransactionScope(_transaction, transactionScopeAsyncFlowOption);
            }
        }

        protected bool IsCompleted { get; private set; }

        public virtual void Complete()
        {
            _transactionScope?.Complete();

            IsCompleted = true;
        }

        public TransactionStatus? TransactionStatus
        {
            get
            {
                if (System.Transactions.Transaction.Current != null)
                {
                    return System.Transactions.Transaction.Current.TransactionInformation.Status;
                }
                return null;
            }
        }

        public IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = CreateNewConnection();
                }

                return _connection;
            }
        }

        protected virtual IDbConnection CreateNewConnection()
        {
            var connection = _connectionFactory();
            connection.Open();
            return connection;
        }

        public IDbTransaction Transaction { get { return null; } }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectionFactory = null;

                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }

                if (_transactionScope != null)
                {
                    _transactionScope.Dispose();
                    _transactionScope = null;
                }

                if (_transaction != null)
                {
                    if (IsCompleted)
                    {
                        _transaction.Commit();
                    }
                    else
                    {
                        _transaction.Rollback();
                    }
                    _transaction.Dispose();
                    _transaction = null;
                }
            }
        }
    }
}
