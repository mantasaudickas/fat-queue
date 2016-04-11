using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FatQueue.Messenger.Core.Expressions
{
    internal class FunctionActionContainer
    {
        public string Type { get; private set; }
        public string Method { get; private set; }
        public Type[] ParameterTypes { get; private set; }
        public string[] Arguments { get; set; }

        public FunctionActionContainer(string type, string method, Type[] parameterTypes, string []arguments)
        {
            Type = type;
            Method = method;
            ParameterTypes = parameterTypes;
            Arguments = arguments;
        }

        public MessageAction Deserialize(ISerializer serializer)
        {
            try
            {
                var type = System.Type.GetType(Type, true, false);
                var parameterTypes = ParameterTypes;
                var method = GetNonOpenMatchingMethod(type, Method, parameterTypes);

                if (method == null)
                {
                    throw new InvalidOperationException(String.Format(
                        "The type `{0}` does not contain a method with signature `{1}({2})`",
                        type.FullName,
                        Method,
                        String.Join(", ", parameterTypes.Select(x => x.Name))));
                }

                var arguments = Arguments;

                return new MessageAction(serializer, type, method, arguments);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not load the MessageAction. See inner exception for the details.", ex);
            }
        }

        public static FunctionActionContainer Serialize(FunctionAction messageAction)
        {
            return new FunctionActionContainer(
                messageAction.Type.AssemblyQualifiedName,
                messageAction.Method.Name,
                messageAction.Method.GetParameters().Select(x => x.ParameterType).ToArray(),
                messageAction.Arguments);
        }

        private static MethodInfo GetNonOpenMatchingMethod(Type type, string name, Type[] parameterTypes)
        {
            var methodCandidates = type.GetMethods();

            foreach (var methodCandidate in methodCandidates)
            {
                if (!methodCandidate.Name.Equals(name, StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = methodCandidate.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    continue;
                }

                var parameterTypesMatched = true;
                var genericArguments = new List<Type>();

                // Determining whether we can use this method candidate with
                // current parameter types.
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var parameterType = parameter.ParameterType;
                    var actualType = parameterTypes[i];

                    // Skipping generic parameters as we can use actual type.
                    if (parameterType.IsGenericParameter)
                    {
                        genericArguments.Add(actualType);
                        continue;
                    }

                    // Skipping non-generic parameters of assignable types.
                    if (parameterType.IsAssignableFrom(actualType)) continue;

                    parameterTypesMatched = false;
                    break;
                }

                if (!parameterTypesMatched) continue;

                // Return first found method candidate with matching parameters.
                return methodCandidate.ContainsGenericParameters
                    ? methodCandidate.MakeGenericMethod(genericArguments.ToArray())
                    : methodCandidate;
            }

            return null;
        }
    }
}