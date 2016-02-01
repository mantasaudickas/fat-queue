using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FatQueue.Messenger.Core.Tools;
using NUnit.Framework;

namespace FatQueue.Messenger.Tests.UnitTests
{
    [TestFixture]
    public class JsonSerializerShould
    {
        [Test]
        public void SerializeDatesToUtc()
        {
            var now = DateTime.Now;
            var data = new DateTimeContainer {Now = now};

            JsonSerializer serializer = new JsonSerializer();
            var content = serializer.Serialize(data);

            var container = (DateTimeContainer) serializer.Deserialize(content, typeof (DateTimeContainer));
            Assert.AreEqual(now, container.Now);
        }

        [Test]
        public void SerializeEnumerable()
        {
            var initialData = new int[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0};
            var values = initialData.Select(i => new DateTimeContainer {Now = DateTime.Now.AddDays(i)});

            var data = new TestEvent
            {
                Values = values
            };

            JsonSerializer serializer = new JsonSerializer();
            var content = serializer.Serialize(data);
            var result = (TestEvent) serializer.Deserialize(content, typeof (TestEvent));
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Values);
            Assert.AreEqual(initialData.Length, result.Values.Count());
        }

        [Test]
        public void SerializeEmptyEnumerable()
        {
            var initialData = new int[] {};
            var values = initialData.Select(i => new DateTimeContainer { Now = DateTime.Now.AddDays(i) });

            var data = new TestEvent
            {
                Values = values.ToList()
            };

            JsonSerializer serializer = new JsonSerializer();
            var content = serializer.Serialize(data);
            var result = (TestEvent)serializer.Deserialize(content, typeof(TestEvent));
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Values);
            Assert.AreEqual(initialData.Length, result.Values.Count());
        }

        [Test]
        public void MatchWhereSelectListIterator()
        {
            var content = ReadResource("WhereSelectListIterator.txt");
            Console.WriteLine(content);

            var isMatch = Regex.IsMatch(content, JsonSerializer.Iterator2Pattern.Item1);
            Assert.IsTrue(isMatch);

            var value = Regex.Replace(content, JsonSerializer.Iterator2Pattern.Item1, JsonSerializer.Iterator2Pattern.Item2);
            Console.WriteLine(value);
        }

        [Test]
        public void MatchWhereSelectArrayIterator()
        {
            var content = ReadResource("WhereSelectArrayIterator.txt");

            var isMatch = Regex.IsMatch(content, JsonSerializer.Iterator2Pattern.Item1);

            Assert.IsTrue(isMatch);
        }

        [Test]
        public void MatchDistinctIterator()
        {
            var content = ReadResource("DistinctIterator.txt");

            var isMatch = Regex.IsMatch(content, JsonSerializer.Iterator1Pattern.Item1);

            Assert.IsTrue(isMatch);
        }

        [Test]
        public void MatchDistinctAnonymousIterator()
        {
            var content = ReadResource("DistinctIteratorAnonymous.txt");

            var isMatch = Regex.IsMatch(content, JsonSerializer.Iterator3Pattern.Item1);

            Assert.IsTrue(isMatch);
        }

        private string ReadResource(string name)
        {
            var assembly = Assembly.GetAssembly(typeof(JsonSerializerShould));
            var resourceName = $"FatQueue.Messenger.Tests.UnitTests.{name}";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                Assert.IsNotNull(stream);

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private class DateTimeContainer
        {
            public DateTime Now { get; set; }
        }

        private class TestEvent
        {
            public IEnumerable<DateTimeContainer> Values { get; set; }
        }
    }
}
