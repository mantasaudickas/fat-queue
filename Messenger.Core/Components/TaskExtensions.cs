using System;
using System.Threading;
using System.Threading.Tasks;

namespace FatQueue.Messenger.Core.Components
{
    public static class TaskExtensions
    {
        public static void Sleep(this TimeSpan timeToDelay, ILogger logger, CancellationToken? cancellationToken = null)
        {
            try
            {
                var token = cancellationToken.GetValueOrDefault(CancellationToken.None);
                if (!token.IsCancellationRequested)
                {
                    Task task = Task.Delay(timeToDelay, token);
                    task.Wait(token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.Error(e.GetFormattedError("Sleep task failed!"));
            }
        }
    }
}
