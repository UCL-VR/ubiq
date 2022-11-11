using System.Linq;
using Ubiq.Logging.Utf8Json.Formatters;
using Ubiq.Logging.Utf8Json.Resolvers.Internal;

namespace Ubiq.Logging.Utf8Json.Resolvers
{
    public static class StandardResolver
    {
        /// <summary>AllowPrivate:False, ExcludeNull:False, NameMutate:Original</summary>
        public static readonly IJsonFormatterResolver Default = DefaultStandardResolver.Instance;
    }
}

namespace Ubiq.Logging.Utf8Json.Resolvers.Internal
{
    internal static class StandardResolverHelper
    {
        internal static readonly IJsonFormatterResolver[] CompositeResolverBase = new[]
        {
            BuiltinResolver.Instance, // Builtin
            EnumResolver.Default,
            CompositeResolver.Instance,
            AttributeFormatterResolver.Instance, // [JsonFormatter]
            DynamicResolver.Instance
        };
    }

    internal sealed class DefaultStandardResolver : IJsonFormatterResolver
    {
        // configure
        public static readonly IJsonFormatterResolver Instance = new DefaultStandardResolver();

        DefaultStandardResolver()
        {
        }

        public IJsonFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.formatter;
        }

        static class FormatterCache<T>
        {
            public static readonly IJsonFormatter<T> formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(object))
                {
                    formatter = null;
                }
                else
                {
                    formatter = InnerResolver.Instance.GetFormatter<T>();
                }
            }
        }

        sealed class InnerResolver : IJsonFormatterResolver
        {
            public static readonly IJsonFormatterResolver Instance = new InnerResolver();

            static readonly IJsonFormatterResolver[] resolvers = StandardResolverHelper.CompositeResolverBase;

            InnerResolver()
            {
            }

            public IJsonFormatter<T> GetFormatter<T>()
            {
                return FormatterCache<T>.formatter;
            }

            static class FormatterCache<T>
            {
                public static readonly IJsonFormatter<T> formatter;

                static FormatterCache()
                {
                    foreach (var item in resolvers)
                    {
                        var f = item.GetFormatter<T>();
                        if (f != null)
                        {
                            formatter = f;
                            return;
                        }
                    }
                }
            }
        }
    }
}