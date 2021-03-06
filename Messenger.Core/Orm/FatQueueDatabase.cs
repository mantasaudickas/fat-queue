﻿using System;
using System.Data;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace FatQueue.Messenger.Core.Orm
{
    public class FatQueueDatabase : IDisposable
    {
        private readonly Func<IDbConnection> _createConnection;
        private readonly ITransaction _externalTransaction;
        private readonly TransactionScope _transactionScope;
        private IDbConnection _connection;

        public FatQueueDatabase(
            Func<IDbConnection> createConnection, 
            TransactionScopeOption? transactionType = null, 
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            ITransaction externalTransaction = null)
        {
            _createConnection = createConnection;
            _externalTransaction = externalTransaction;

            if (!transactionType.HasValue)
                return;

            var transactionTypeValue = transactionType.Value;

            if (Transaction.Current != null && transactionTypeValue == TransactionScopeOption.Required)
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
                if (_externalTransaction != null)
                {
                    return _externalTransaction.Connection;
                }

                if (_connection == null)
                {
                    _connection = _createConnection();
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
