using System;
using System.Data.SqlClient;
using System.Transactions;
using FatQueue.Messenger.Tests.Events;

namespace FatQueue.Messenger.Tests.Handlers
{
    public class FatQueueFailingEventHandler : IHandler<FatQueueFailingEvent>
    {
        public void Handle(FatQueueFailingEvent request)
        {
            try
            {
                using (var scope = new TransactionScope(TransactionScopeOption.Required))
                {
                    using (var connection = new SqlConnection("Server=.\\SQLEXPRESS;Database=Messenger;Integrated Security=SSPI"))
                    {
                        connection.Open();

                        if (request.Fail)
                        {
                            throw new Exception("This event failed!");
                        }
                    }

                    scope.Complete();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
