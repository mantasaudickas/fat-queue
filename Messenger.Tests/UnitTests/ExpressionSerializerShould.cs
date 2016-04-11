using System;
using System.Linq.Expressions;
using FatQueue.Messenger.Core.Expressions;
using FatQueue.Messenger.Core.Tools;
using NUnit.Framework;

namespace FatQueue.Messenger.Tests.UnitTests
{
    [TestFixture]
    public class ExpressionSerializerShould
    {
        [Test]
        public void SerializeActionExpression()
        {
            var serializer = new JsonSerializer();

            var expression = Publish<TestExecutor>(executor => executor.Execute(15), serializer);
            Console.WriteLine(expression);

            var messageActionContainer = (MessageActionContainer)serializer.Deserialize(expression, typeof(MessageActionContainer));
            var messageAction = messageActionContainer.DeserializeAction(serializer);
            messageAction.Perform(new JobActivator());
        }

        [Test]
        public void SerializeFunctionExpression()
        {
            var serializer = new JsonSerializer();

            var expression = Serialize<ContextProvider, IDisposable>(factory => factory.Create(15), serializer);
            Console.WriteLine(expression);

            var messageActionContainer = (MessageActionContainer)serializer.Deserialize(expression, typeof(MessageActionContainer));
            var messageAction = messageActionContainer.DeserializeFunction(serializer);
            var scope = messageAction.Perform<IDisposable>(new JobActivator());
            Assert.IsNotNull(scope);
            Assert.IsTrue(scope is Context);
            scope.Dispose();
        }

        private string Publish<T>(Action<T> action, JsonSerializer serializer)
        {
            return Serialize<T>(o => action(o), serializer);
        }

        private string Serialize<T>(Expression<Action<T>> action, JsonSerializer serializer)
        {
            ExpressionSerializer expressionSerializer = new ExpressionSerializer(serializer);
            return expressionSerializer.Serialize(action);
        }

        private string Serialize<T, R>(Expression<Func<T, R>> action, JsonSerializer serializer) where R : IDisposable
        {
            ExpressionSerializer expressionSerializer = new ExpressionSerializer(serializer);
            return expressionSerializer.SerializeFactory(action);
        }

        public class TestExecutor
        {
            public void Execute(int arguments)
            {
                Console.WriteLine(arguments);
            }
        }

        private class ContextProvider
        {
            public IDisposable Create(int userId)
            {
                return new Context(userId);
            }
        }

        private class Context : IDisposable
        {
            private readonly int _userId;

            public Context(int userId)
            {
                _userId = userId;
            }

            public void Dispose()
            {
                Console.WriteLine(_userId);
            }
        }

    }
}
