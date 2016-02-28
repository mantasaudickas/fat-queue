using System.Configuration;
using System.Web.Mvc;
using FatQueue.Messenger.Core;
using FatQueue.Messenger.Core.Tools;
using FatQueue.Messenger.MsSql;
using Messenger.Web.Models;

namespace Messenger.Web.Controllers
{
    public class MessengerController : Controller
    {
        private readonly IMessengerService _messengerService;
        public MessengerController()
        {
            var settings = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            var connectionString = settings.ConnectionString;

            _messengerService = new MessengerService(connectionString, new TraceLogger(false));
        }

        // GET: Messenger
        public ActionResult Index()
        {
            var model = new MainViewModel
            {
                Queues = _messengerService.GetQueueStatuses(),
                Status = _messengerService.GetMessengerStatus()
            };

            return View(model);
        }

        // GET: Messenger/Active
        public ActionResult Active()
        {
            var model = new ActiveProcessesModel
            {
                ActiveProcesses = _messengerService.GetActiveProcesses()
            };

            return View(model);
        }

        public ActionResult ReleaseProcessLock(string processname)
        {
            _messengerService.ReleaseProcessLock(processname);
            return RedirectToAction("Index");
        }

        // GET: Messenger/FailedMessages?pageNo=1&pageSize=20
        public ActionResult FailedMessages(int pageNo = 1, int pageSize = 20)
        {
            if (pageNo < 1)
            {
                pageNo = 1;
            }

            var model = new FailedMessageModel
            {
                PageNo = pageNo,
                PageSize = pageSize,
                Messages = _messengerService.GetFailedMessages(pageNo, pageSize, null, null)
            };

            return View(model);
        }

        public ActionResult RecoverFailedMessages()
        {
            _messengerService.RecoverFailedMessages();
            return RedirectToAction("FailedMessages");
        }

        public ActionResult RecoverFailedMessage(int messageId)
        {
            _messengerService.ReenqueueFailedMessages(new[] {messageId});
            return RedirectToAction("FailedMessages");
        }
    }
}