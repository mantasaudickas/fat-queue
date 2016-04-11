using System;
using FatQueue.Messenger.Core.Expressions;

namespace FatQueue.Messenger.Core
{
    public class ExecutionContext : IDisposable
    {
        ~ExecutionContext()
        {
            Dispose(false);
        }

        public void Invoke(ExpressionExecutor executor, string messageContent)
        {
            Exception exception = null;
            try
            {
                OnBeforeExecution();
                executor.Execute(messageContent);
            }
            catch (Exception e)
            {
                exception = e;
                throw;
            }
            finally
            {
                OnAfterExecution(exception);
            }
        }

        protected virtual void OnBeforeExecution()
        {
        }

        protected virtual void OnAfterExecution(Exception exception)
        {
        }

        public void Dispose()
        {
           Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
