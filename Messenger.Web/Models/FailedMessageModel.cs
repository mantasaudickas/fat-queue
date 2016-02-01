using System.Collections.Generic;
using FatQueue.Messenger.Core;

namespace Messenger.Web.Models
{
    public class FailedMessageModel
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<FailedMessage> Messages { get; set; }
    }
}