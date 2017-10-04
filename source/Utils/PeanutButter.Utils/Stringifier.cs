using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable IntroduceOptionalParameters.Global

namespace PeanutButter.Utils
{
    /// <summary>
    /// Provides convenience functions to get reasonable string representations of objects and collections
    /// </summary>
#if BUILD_PEANUTBUTTER_INTERNAL
    internal
#else
    public 
#endif
        static class Stringifier
    {
        /// <summary>
        /// Provides a reasonable human-readable string representation of a collection
        /// </summary>
        /// <param name="objs"></param>
        /// <returns>Human-readable representation of collection</returns>
        public static string Stringify<T>(IEnumerable<T> objs)
        {
            return StringifyCollectionInternal(objs);
        }

        private static string StringifyCollectionInternal<T>(IEnumerable<T> objs)
        {
            return objs == null
                ? "(null collection)"
                : $"[ {string.Join(", ", objs.Select(o => Stringify(o)))} ]";
        }

        /// <summary>
        /// Provides a reasonable human-readable string representation of an object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>Human-readable representation of object</returns>
        public static string Stringify(object obj)
        {
            return Stringify(obj, "null");
        }

        /// <summary>
        /// Provides a reasonable human-readable string representation of an object
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="nullRepresentation">How to represent null values - defaults to the string "null"</param>
        /// <returns>Human-readable representation of object</returns>
        public static string Stringify(object obj, string nullRepresentation)
        {
            return SafeStringifier(obj, 0, nullRepresentation ?? "null");
        }

        private const int MaxStringifyDepth = 10;
        private const int IndentSize = 2;

        private static readonly Dictionary<Type, Func<object, string>> _primitiveStringifiers
            = new Dictionary<Type, Func<object, string>>()
            {
                [typeof(string)] = o => $"\"{o}\"",
                [typeof(bool)] = o => o.ToString().ToLower()
            };

        private static readonly string[] _ignoreAssembliesByName =
        {
            "mscorlib"
        };

        private static readonly Tuple<Func<object, int, bool>, Func<object, int, string, string>>[]
            _strategies =
            {
                MakeStrategy(IsNull, PrintNull),
                MakeStrategy(IsPrimitive, StringifyPrimitive),
                MakeStrategy(IsEnum, StringifyEnum),
                MakeStrategy(IsEnumerable, StringifyCollection),
                MakeStrategy(Default, StringifyJsonLike),
                MakeStrategy(LastPass, JustToStringIt)
            };

        private static string StringifyCollection(object obj, int level, string nullRep)
        {
            var itemType = obj.GetType().TryGetEnumerableItemType() 
                ?? throw new Exception($"{obj.GetType()} is not IEnumerable<T>");
            var method = typeof(Stringifier)
                .GetMethod(nameof(StringifyCollectionInternal), BindingFlags.NonPublic | BindingFlags.Static);
            var specific = method.MakeGenericMethod(itemType);
            return (string)(specific.Invoke(null, new[] { obj }));
        }

        private static bool IsEnumerable(object obj, int level)
        {
            return obj.GetType().ImplementsEnumerableGenericType();
        }

        private static string StringifyEnum(object obj, int level, string nullRepresentation)
        {
            return obj.ToString();
        }

        private static bool IsEnum(object obj, int level)
        {
#if NETSTANDARD1_6
            return obj.GetType().GetTypeInfo().IsEnum;
#else
            return obj.GetType().IsEnum;
#endif
        }

        private static string JustToStringIt(object obj, int level, string nullRepresentation)
        {
            try
            {
                return obj.ToString();
            }
            catch
            {
                return $"{{{obj.GetType()}}}";
            }
        }

        private static bool LastPass(object arg1, int arg2)
        {
            return true;
        }

        private static string PrintNull(object obj, int level, string nullRepresentation)
        {
            return nullRepresentation;
        }

        private static bool IsNull(object obj, int level)
        {
            return obj == null;
        }

        private static Tuple<Func<object, int, bool>, Func<object, int, string, string>> MakeStrategy(
            Func<object, int, bool> matcher, Func<object, int, string, string> writer
        )
        {
            return Tuple.Create(matcher, writer);
        }

        private static bool IsPrimitive(object obj, int level)
        {
            return level >= MaxStringifyDepth ||
                   Types.PrimitivesAndImmutables.Contains(obj.GetType());
        }

        private static bool Default(object obj, int level)
        {
            return true;
        }

        private static string SafeStringifier(object obj, int level, string nullRepresentation)
        {
            if (level >= MaxStringifyDepth)
            {
                return StringifyPrimitive(obj, level, nullRepresentation);
            }
            return _strategies.Aggregate(null as string,
                (acc, cur) => acc ??
                              ApplyStrategy(
                                  cur.Item1,
                                  cur.Item2,
                                  obj,
                                  level,
                                  nullRepresentation
                              )
            );
        }

        private static string ApplyStrategy(
            Func<object, int, bool> matcher,
            Func<object, int, string, string> strategy,
            object obj,
            int level,
            string nullRepresentation)
        {
            try
            {
                return matcher(obj, level) 
                        ? strategy(obj, level, nullRepresentation)
                        : null;
            }
            catch
            {
                return null;
            }
        }


        private static string StringifyJsonLike(object obj, int level, string nullRepresentation)
        {
            var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var indentMinus1 = new string(' ', level * IndentSize);
            var indent = indentMinus1 + new string(' ', IndentSize);
            var joinWith = props.Aggregate(new List<string>(), (acc, cur) =>
                {
                    var propValue = cur.GetValue(obj);
                    if (_ignoreAssembliesByName.Contains(
#if NETSTANDARD1_6
                            cur.DeclaringType?.AssemblyQualifiedName.Split(
                            new[] { "," }, StringSplitOptions.RemoveEmptyEntries
                        ).Skip(1).FirstOrDefault()
#else
                        cur.DeclaringType?.Assembly.GetName().Name
#endif
                    ))
                    {
                        acc.Add(string.Join("", cur.Name, ": ", propValue?.ToString()));
                    }
                    else
                    {
                        acc.Add(string.Join(
                            "",
                            cur.Name,
                            ": ",
                            SafeStringifier(propValue, level + 1, nullRepresentation)));
                    }

                    return acc;
                })
                .JoinWith($"\n{indent}");
            return ("{\n" +
                    string.Join(
                        "\n{indent}",
                        $"{indent}{joinWith}"
                    ) +
                    $"\n{indentMinus1}}}").Compact();
        }

        private static string StringifyPrimitive(object obj, int level, string nullRep)
        {
            if (obj == null)
                return nullRep;
            return _primitiveStringifiers.TryGetValue(obj.GetType(), out var strategy)
                ? strategy(obj)
                : obj.ToString();
        }
    }

    internal static class StringifierStringExtensions
    {
        internal static string Compact(this string str)
        {
            return new[]
                {
                    "\r\n",
                    "\n"
                }.Aggregate(str, (acc, cur) =>
                {
                    var twice = $"{cur}{cur}";
                    while (acc.Contains(twice))
                        acc = acc.Replace(twice, "");
                    return acc;
                })
                .SquashEmptyObjects();
        }

        private static string SquashEmptyObjects(this string str)
        {
            return str.RegexReplace("{\\s*}", "{}");
        }
    }
}