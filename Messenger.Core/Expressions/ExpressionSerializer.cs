using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Mvc.ExpressionUtil;

namespace FatQueue.Messenger.Core.Expressions
{
    public class ExpressionSerializer
    {
        private static readonly ParameterExpression UnusedParameterExpr = Expression.Parameter(typeof(object), "_unused");
        private readonly ISerializer _serializer;

        public ExpressionSerializer(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public string Serialize<T>(Expression<Action<T>> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException(string.Format("Expression body should be `MethodCallExpression`. Received: {0}", methodCall.GetType()));
            }

            var messageAction = new MessageAction(_serializer, typeof(T), callExpression.Method, GetExpressionArguments(callExpression));
            var messageActionContainer = MessageActionContainer.Serialize(messageAction);
            var result = _serializer.Serialize(messageActionContainer);

            return result;
        }

        public string SerializeFactory<T, TResult>(Expression<Func<T, TResult>> methodCall)
        {
            if (methodCall == null) throw new ArgumentNullException("methodCall");

            var callExpression = methodCall.Body as MethodCallExpression;
            if (callExpression == null)
            {
                throw new NotSupportedException("Expression body should be `NewExpression`");
            }

            var messageAction = new FunctionAction(_serializer, typeof(T), callExpression.Method, GetExpressionArguments(callExpression));
            var messageActionContainer = MessageActionContainer.Serialize(messageAction);
            var result = _serializer.Serialize(messageActionContainer);

            return result;
        }

        private string[] GetExpressionArguments(MethodCallExpression callExpression)
        {
            var arguments = callExpression.Arguments.Select(GetExpressionArgumentValue).ToArray();

            var serializedArguments = new List<string>(arguments.Length);
            foreach (var argument in arguments)
            {
                string value = null;

                if (argument != null)
                {
                    if (argument is DateTime)
                    {
                        value = ((DateTime)argument).ToString("o", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        value = _serializer.Serialize(argument);
                    }
                }

                // Logic, related to optional parameters and their default values, 
                // can be skipped, because it is impossible to omit them in 
                // lambda-expressions (leads to a compile-time error).

                serializedArguments.Add(value);
            }

            return serializedArguments.ToArray();
        }

        private object GetExpressionArgumentValue(Expression expression)
        {
            var constantExpression = expression as ConstantExpression;

            if (constantExpression != null)
            {
                return constantExpression.Value;
            }

            return Evaluate(expression);
        }

        private object Evaluate(Expression arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            Func<object, object> func = Wrap(arg);
            return func(null);
        }

        private Func<object, object> Wrap(Expression arg)
        {
            var lambdaExpr = Expression.Lambda<Func<object, object>>(Expression.Convert(arg, typeof(object)), UnusedParameterExpr);
            return CachedExpressionCompiler.Process(lambdaExpr);
        }
    }
}
