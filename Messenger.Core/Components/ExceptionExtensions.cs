using System;
using System.Text;

namespace FatQueue.Messenger.Core.Components
{
    public static class ExceptionExtensions
    {
        public static string GetFormattedError(this Exception exc, int? id = null, bool isQueueId = false)
        {
            return GetFormattedError(exc, null, id, isQueueId);
        }

        public static string GetFormattedError(this Exception exc, string message, int? id = null, bool isQueueId = false)
        {
            try
            {
                string exception = exc.ToString();

                var errorMessage = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    errorMessage.AppendLine(message);
                }

                if (id.GetValueOrDefault() > 0)
                {
                    if (isQueueId)
                    {
                        errorMessage.AppendFormat("Queue [{0}] processing failed.", id).AppendLine();
                    }
                    else
                    {
                        errorMessage.AppendFormat("Message [{0}] processing failed.", id).AppendLine();
                    }
                }

                errorMessage.Append(exception).AppendLine();
            
                string errorMessageString = errorMessage.ToString();
                return errorMessageString;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
