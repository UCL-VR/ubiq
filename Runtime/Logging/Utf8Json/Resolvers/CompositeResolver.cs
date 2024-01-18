﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Ubiq.Logging.Utf8Json.Resolvers
{
    public sealed class CompositeResolver : IJsonFormatterResolver
    {
        public static readonly CompositeResolver Instance = new CompositeResolver();

        static bool isFreezed = false;
        static List<IJsonFormatter> formatters = new List<IJsonFormatter>();
        static List<IJsonFormatterResolver> resolvers = new List<IJsonFormatterResolver>();

        CompositeResolver()
        {
        }

        public static void Register(params IJsonFormatterResolver[] resolvers)
        {
            if (isFreezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            CompositeResolver.resolvers.AddRange(resolvers);
        }

        public static void Register(params IJsonFormatter[] formatters)
        {
            if (isFreezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            CompositeResolver.formatters.AddRange(formatters);
        }

        public static void Register(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            if (isFreezed)
            {
                throw new InvalidOperationException("Register must call on startup(before use GetFormatter<T>).");
            }

            CompositeResolver.resolvers.AddRange(resolvers);
            CompositeResolver.formatters.AddRange(formatters);
        }

        public static void RegisterAndSetAsDefault(params IJsonFormatterResolver[] resolvers)
        {
            Register(resolvers);
            JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
        }

        public static void RegisterAndSetAsDefault(params IJsonFormatter[] formatters)
        {
            Register(formatters);
            JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
        }

        public static void RegisterAndSetAsDefault(IJsonFormatter[] formatters, IJsonFormatterResolver[] resolvers)
        {
            Register(formatters);
            Register(resolvers);
            JsonSerializer.SetDefaultResolver(CompositeResolver.Instance);
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
                isFreezed = true;

                foreach (var item in formatters)
                {
                    foreach (var implInterface in item.GetType().GetTypeInfo().ImplementedInterfaces)
                    {
                        var ti = implInterface.GetTypeInfo();
                        if (ti.IsGenericType && ti.GenericTypeArguments[0] == typeof(T))
                        {
                            formatter = (IJsonFormatter<T>)item;
                            return;
                        }
                    }
                }

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