using System;
using System.Transactions;

namespace FatQueue.Messenger.MsSql.Orm
{
    internal class Transaction : ITransaction
    {
        private CommittableTransaction _transaction;
        private TransactionScope _transactionScope;
        private bool _isCompleted;

        public Transaction(
            IsolationLevel isolationLevel, 
            TimeSpan? timeout, 
            TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required, 
            TransactionScopeAsyncFlowOption transactionScopeAsyncFlowOption = TransactionScopeAsyncFlowOption.Enabled)
        {
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

        public void Complete()
        {
            _transactionScope?.Complete();

            _isCompleted = true;
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

        public void Dispose()
        {
            if (_transactionScope != null)
            {
                _transactionScope.Dispose();
                _transactionScope = null;
            }

            if (_transaction != null)
            {
                if (_isCompleted)
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

            GC.SuppressFinalize(this);
        }
    }
}
