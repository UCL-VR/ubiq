using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Logging.Utf8Json.Resolvers
{
    public class UbiqResolver : IJsonFormatterResolver
    {
        public static UbiqResolver Instance = new UbiqResolver();

        public IJsonFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IJsonFormatter<T> formatter;

            static FormatterCache()
            {
                // Reduce IL2CPP code generate size(don't write long code in <T>)
                formatter = (IJsonFormatter<T>)GetFormatter(typeof(T));
            }
        }

        static readonly Dictionary<Type, object> formatterMap = new Dictionary<Type, object>()
        {
            {typeof(NetworkId), NetworkIdFormatter.Default}
        };

        internal static object GetFormatter(Type t)
        {
            object formatter;
            if (formatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }

        internal class NetworkIdFormatter : IJsonFormatter<NetworkId>
        {
            public static NetworkIdFormatter Default = new NetworkIdFormatter();

            public NetworkId Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                throw new NotImplementedException();
            }

            public void Serialize(ref JsonWriter writer, NetworkId value, IJsonFormatterResolver formatterResolver)
            {
                writer.WriteString(value.ToString());
            }
        }
    }
}