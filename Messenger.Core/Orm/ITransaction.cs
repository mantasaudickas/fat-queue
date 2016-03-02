using System;
using System.Data;
using System.Transactions;

namespace FatQueue.Messenger.Core.Orm
{
    public interface ITransaction : IDisposable
    {
        void Complete();
        TransactionStatus? TransactionStatus { get; }
        IDbConnection Connection { get; }
        IDbTransaction Transaction { get; }
    }
}
