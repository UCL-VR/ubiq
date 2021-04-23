using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Logging.Utf8Json.Resolvers
{
    /// <summary>
    /// Fallback resolver for unknown types.
    /// </summary>
    /// <remarks>
    /// This currently uses the JsonUtility as a fallback, though it should also be possible to build formatters dynamically,
    /// without AOT, using delegates.
    /// </remarks>
    public class DynamicResolver : IJsonFormatterResolver
    {
        public static IJsonFormatterResolver Instance = new DynamicResolver();

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
                formatter = new DynamicFormatter<T>();
            }
        }

        internal class DynamicFormatter<T> : IJsonFormatter<T>
        {
            public DynamicFormatter()
            {

            }

            public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                throw new System.NotImplementedException();
            }

            public void Serialize(ref JsonWriter writer, T value, IJsonFormatterResolver formatterResolver)
            {
                writer.WriteJson(JsonUtility.ToJson(value));
            }
        }
    }
}