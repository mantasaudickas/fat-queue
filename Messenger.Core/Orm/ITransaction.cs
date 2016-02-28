using System;
using System.Transactions;

namespace FatQueue.Messenger.Core.Orm
{
    public interface ITransaction : IDisposable
    {
        void Complete();
        TransactionStatus? TransactionStatus { get; }
    }
}
