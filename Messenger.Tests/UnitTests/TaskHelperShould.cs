using System;
using System.Collections.Generic;
using System.Diagnostics;
using FatQueue.Messenger.Core.Components;
using FatQueue.Messenger.Core.Tools;
using NUnit.Framework;

namespace FatQueue.Messenger.Tests.UnitTests
{
    [TestFixture]
    public class TaskHelperShould
    {
        [Test]
        public void Sleep()
        {
            var timeToDelay = TimeSpan.FromSeconds(30);

            var timer = Stopwatch.StartNew();

            timeToDelay.Sleep(new TraceLogger(true));
            
            timer.Stop();
            Console.WriteLine(timer.Elapsed);
            Assert.GreaterOrEqual(timer.Elapsed, timeToDelay);
        }

        [Test]
        public void Test()
        {
            var dic = new Dictionary<string, object>
            {
                {"one", new object()},
                {"two", new object()}
            };

            foreach (var d in dic)
            {
                dic[d.Key] = new object();
            }
        }
    }
}
