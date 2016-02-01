using System;

namespace FatQueue.Messenger.Core.Components
{
    public static class TypeExtensions
    {
        public static string GetContentType(this Type type)
        {
            string assemblyName = type.AssemblyQualifiedName;

            if (assemblyName != null)
            {
                int index = assemblyName.IndexOf(',');
                if (index > 0)
                    index = assemblyName.IndexOf(',', index + 1);

                assemblyName = assemblyName.Substring(0, index);

                return assemblyName;
            }

            return type.FullName;
        }
    }
}
