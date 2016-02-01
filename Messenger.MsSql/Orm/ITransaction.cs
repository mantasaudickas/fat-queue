using System;
using System.Transactions;

namespace FatQueue.Messenger.MsSql.Orm
{
    internal interface ITransaction : IDisposable
    {
        void Complete();
        TransactionStatus? TransactionStatus { get; }
    }
}
