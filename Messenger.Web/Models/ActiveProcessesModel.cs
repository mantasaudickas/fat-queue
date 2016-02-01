using System.Collections.Generic;
using ProcessStatus = FatQueue.Messenger.Core.ProcessStatus;

namespace Messenger.Web.Models
{
    public class ActiveProcessesModel
    {
        public IEnumerable<ProcessStatus> ActiveProcesses { get; set; }
    }
}