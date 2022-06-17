# Serialiser

The logging functionality uses a custom Json serialiser that facilitates building Json objects across multiple function calls.

This is based on [neuecc's Utf8Json](https://github.com/neuecc/Utf8Json), but with modifications to track memory usage and remove code generation requirements.

The Utf8Json serialiser is in the `Ubiq.Logging.Utf8Json` namspace. It is not recommended to use the serialiser for purposes other than logging; import an unmodified version of the library separately instead.

## Formatters

Libraries such as Utf8Json typically have methods that serialise and deseralise specific types by sequentially reading and writing tokens to and from streams. (In this case, the tokes are read and written using the `JsonReader` and `JsonWriter` structures.)

Utf8Json finds the appropriate method to use using `FormatterResovler` classes. These classes return a cached `Formatter<T>` class, which is an object with two methods to read and write objects of type `T` as Json.

The included version of Utf8Json includes formatters for a number of known types, including all the basic primitives, and enums. Enums are serialised as names. 

### Code Generation

To serialise types that do not have an explicit formatter defined, libraries such as Utf8Json usually build serialisation methods at runtime using code generation. This is not supported on platforms that use [IL2CPP](https://docs.unity3d.com/Manual/ScriptingRestrictions.html) however. 

To avoid code generation, unknown types are serialised by the Unity JsonUtility and embedded as objects.

### Resolvers and Formatters

When a type is serialised, Utf8Json will use the `DefaultResolver` to find a formatter. The `DefaultResolver` is defined in the `JsonSerializer` class as a static member and returns a `StandardResolver`, a type of composite resolver. This resolver will search each resolver registered to it in turn, and return the first `Formatter` that matches the type. The `StandardResolver` includes formatters for the built-in types, and the dynamic formatter fallback.

### Caching

Utf8Json makes common use of the following design pattern.

```
public IJsonFormatter<T> GetFormatter<T>()
{
	return FormatterCache<T>.formatter;
}

static class FormatterCache<T>
{
	public static readonly IJsonFormatter<T> formatter;

	static FormatterCache()
	{
		formatter = (IJsonFormatter<T>)BuiltinResolverGetFormatterHelper.GetFormatter(typeof(T));
	}
}
```

This snippet leverages the behaviour of generics in C# to replace formatter references in code, without using code generation.
In C#, when a generic type is [first constructed](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/generics/generics-in-the-run-time), the runtime will produce the concrete type and substitute it in the appropriate locations in the MSIL.
The static constructor is [called]((https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors)) before the formatter is referenced for the first time.

That is, the generic FormatterCache<T> type is replaced in the MSIL and the formatter member it returns is resolved on demand (when the `FormatterCache<T>` is first constructed).

# Memory Management

The Utf8Json namespace manages its own global memory pools to minimise GC allocations. It does not track memory usage directly however. 

Instead, `LogCollector` instances track how many bytes of pooled memory they have in their queues at any time, and use this to control whether new events are buffered or dropped.

Memory is rented from the pool on demand by JsonWriter objects created by `LogEmitter` instances. Outstanding memory is returned to the pool when a JsonWriter is disposed. JsonWriters are disposed by the `LogCollector` they are fed to, either after being copied for transmission or discarded when the buffer reaches capacity. `LogEmitter` instances only create JsonWriters if a `LogCollector` has been registered to recieve (and dispose of) the completed object.