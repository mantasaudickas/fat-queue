using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using FatQueue.Messenger.MsSql;
using FatQueue.Messenger.Tests.Events;
using FatQueue.Messenger.Tests.Handlers;

namespace FatQueue.Messenger.Tests.Executor
{
    public class LongRunningTestExecutor
    {
        private static readonly string[] Queues = {"Q0", "Q1", "Q2", "Q3", "Q4"};

        public void Execute(MsSqlSettings clientSettings)
        {
            Task.Factory.StartNew(() => ProcessTasks(clientSettings));
        }

        private void ProcessTasks(MsSqlSettings clientSettings)
        {
            var timer = Stopwatch.StartNew();

            var taskList = new List<Task>();
            for (int i = 0; i < 10; ++i)
            {
                int processId = i;
                var task = Task.Factory.StartNew(
                    () => TaskProcess(processId, clientSettings),
                    TaskCreationOptions.LongRunning);
                taskList.Add(task);
            }

            while (taskList.Count > 0)
            {
                for (int i = taskList.Count - 1; i >= 0; i--)
                {
                    var task = taskList[i];
                    if (task.IsCanceled)
                    {
                        Console.WriteLine("Task canceled!");
                        taskList.RemoveAt(i);
                    }
                    if (task.IsFaulted)
                    {
                        Console.WriteLine("Task faulted!");

                        taskList.RemoveAt(i);
                    }

                    if (task.IsCompleted)
                    {
                        taskList.RemoveAt(i);
                    }
                }
            }

            timer.Stop();

            Console.WriteLine("All tasks finished. Time: " + timer.Elapsed);
        }

        private void TaskProcess(int processId, MsSqlSettings clientSettings)
        {
            int queueCount = Queues.Length;
            for (int i = 0; i < 10000; ++i)
            {
                int messageId = i;
                var messengerClient = new MsSqlMessenger(clientSettings);
                messengerClient.Publish<FatQueuePrintMessageEventHandler>(handler => handler.Handle(
                    new FatQueuePrintMessageEvent
                    {
                        Message = new CustomMessage {Message = $"[{processId.ToString().PadLeft(3, '0')}] - {messageId}"}
                    }), Queues[processId % queueCount]);
            }
        }
    }
}
