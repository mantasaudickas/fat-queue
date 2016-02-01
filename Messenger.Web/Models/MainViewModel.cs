using System.Collections.Generic;
using FatQueue.Messenger.Core;

namespace Messenger.Web.Models
{
    public class MainViewModel
    {
        public IEnumerable<QueueStatus> Queues { get; set; }
        public IEnumerable<MessengerStatus> Status { get; set; }
    }
}