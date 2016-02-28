using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace FatQueue.Messenger.Core.Tools
{
    public class JsonSerializer : ISerializer
    {
        public static readonly Tuple<string, string> Iterator1Pattern = new Tuple<string, string>(
            "System.Linq.Enumerable\\+(.+?)Iterator(.+?)`1\\[\\[(.+?)\\]\\], System.Core",
            "System.Collections.Generic.List`1[[$3]], mscorlib");

        public static readonly Tuple<string, string> Iterator2Pattern = new Tuple<string, string>(
            "System.Linq.Enumerable\\+(.+?)Iterator`2\\[\\[(.+?)\\],\\[(.+?)\\]\\], System.Core",
            "System.Collections.Generic.List`1[[$3]], mscorlib");

        public static readonly Tuple<string, string> Iterator3Pattern = new Tuple<string, string>(
            "System.Linq.Enumerable\\+(.+?)Iterator(.+?)`2\\[\\[(.+?)\\],\\[(.+?)\\]\\], System.Core",
            "System.Collections.Generic.List`1[[$4]], mscorlib");

        private static readonly List<Tuple<string, string>> Patterns = new List<Tuple<string, string>>
        {
            Iterator1Pattern,
            Iterator2Pattern,
            Iterator3Pattern,
        };

        private readonly bool _enableEnumerableFix;

        public JsonSerializer() : this(true)
        {
        }

        public JsonSerializer(bool enableEnumerableFix)
        {
            _enableEnumerableFix = enableEnumerableFix;
        }

        public string Serialize<T>(T data)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            return JsonConvert.SerializeObject(data, settings);
        }

        public object Deserialize(string value, Type instanceType)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            if (_enableEnumerableFix)
            {
                System.Diagnostics.Debug.WriteLine(value, "FatQueue.JsonSerializer.BeforeEnumerableFix");

                foreach (var pattern in Patterns)
                {
                    value = Regex.Replace(value, pattern.Item1, pattern.Item2);
                }

                System.Diagnostics.Debug.WriteLine(value, "FatQueue.JsonSerializer.AfterEnumerableFix");
            }

            return JsonConvert.DeserializeObject(value, instanceType, settings);
        }

        public static string ToJson<T>(T data)
        {
            return new JsonSerializer().Serialize(data);
        }
    }
}
