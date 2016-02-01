using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;

namespace FatQueue.Messenger.Core.Expressions
{
    internal class MessageAction
    {
        private readonly ISerializer _serializer;
        public Type Type { get; set; }
        public MethodInfo Method { get; set; }
        public string[] Arguments { get; set; }

        public MessageAction(ISerializer serializer, Type type, MethodInfo method, string[] arguments)
        {
            _serializer = serializer;
            Type = type;
            Method = method;
            Arguments = arguments;
            if (type == null) throw new ArgumentNullException("type");
            if (method == null) throw new ArgumentNullException("method");
            if (arguments == null) throw new ArgumentNullException("arguments");

            if (method.ContainsGenericParameters)
            {
                throw new ArgumentException("MessageAction method can not contain unassigned generic type parameters.", "method");
            }

            Validate();
        }

        public object Perform(IJobActivator activator)
        {
            if (activator == null) throw new ArgumentNullException("activator");

            object instance = null;

            object result;
            try
            {
                if (!Method.IsStatic)
                {
                    instance = Activate(activator);
                }

                var deserializedArguments = DeserializeArguments();
                result = InvokeMethod(instance, deserializedArguments);
            }
            finally
            {
                Dispose(instance);
            }

            return result;
        }

        private object InvokeMethod(object instance, object[] deserializedArguments)
        {
            try
            {
                return Method.Invoke(instance, deserializedArguments);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    // `OperationCanceledException` and its descendants are used
                    // to notify a worker that job performance was canceled,
                    // so we should not wrap this exception and throw it as-is.
                    throw ex.InnerException;
                }

                throw new Exception(
                    "An exception occurred during performance of the job.",
                    ex.InnerException);
            }
        }

        private static void Dispose(object instance)
        {
            try
            {
                var disposable = instance as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "MessageAction has been performed, but an exception occurred during disposal.",
                    ex);
            }
        }

        private object Activate(IJobActivator activator)
        {
            try
            {
                var instance = activator.ActivateJob(Type);

                if (instance == null)
                {
                    throw new InvalidOperationException(
                        String.Format("JobActivator returned NULL instance of the '{0}' type.", Type));
                }

                return instance;
            }
            catch (Exception ex)
            {
                throw new Exception("An exception occurred during job activation.", ex);
            }
        }

        private void Validate()
        {
            if (Method.DeclaringType == null)
            {
                throw new NotSupportedException("Global methods are not supported. Use class methods instead.");
            }

            if (!Method.DeclaringType.IsAssignableFrom(Type))
            {
                throw new ArgumentException(String.Format(
                    "The type `{0}` must be derived from the `{1}` type.",
                    Method.DeclaringType,
                    Type));
            }

            if (!Method.IsPublic)
            {
                throw new NotSupportedException("Only public methods can be invoked in the background.");
            }

            if (typeof(Task).IsAssignableFrom(Method.ReturnType))
            {
                throw new NotSupportedException("Async methods are not supported. Please make them synchronous before using them in background.");
            }

            var parameters = Method.GetParameters();

            if (parameters.Length != Arguments.Length)
            {
                throw new ArgumentException("Argument count must be equal to method parameter count.");
            }

            foreach (var parameter in parameters)
            {
                // There is no guarantee that specified method will be invoked
                // in the same process. Therefore, output parameters and parameters
                // passed by reference are not supported.

                if (parameter.IsOut)
                {
                    throw new NotSupportedException(
                        "Output parameters are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }

                if (parameter.ParameterType.IsByRef)
                {
                    throw new NotSupportedException(
                        "Parameters, passed by reference, are not supported: there is no guarantee that specified method will be invoked inside the same process.");
                }
            }
        }

        private object[] DeserializeArguments()
        {
            try
            {
                var parameters = Method.GetParameters();
                var result = new List<object>(Arguments.Length);

                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argument = Arguments[i];

                    object value;

                    try
                    {
                        value = argument != null
                            ? _serializer.Deserialize(argument, parameter.ParameterType)
                            : null;
                    }
                    catch (Exception)
                    {
                        if (parameter.ParameterType == typeof (object))
                        {
                            // Special case for handling object types, because string can not
                            // be converted to object type.
                            value = argument;
                        }
                        else
                        {
                            var converter = TypeDescriptor.GetConverter(parameter.ParameterType);
                            value = converter.ConvertFromInvariantString(argument);
                        }
                    }

                    result.Add(value);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "An exception occurred during arguments deserialization.",
                    ex);
            }
        }
    }
}