using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AutomaticWip.Core
{
    public static partial class Settings
    {
        /// <summary>
        /// Cache the enum/fields/attributes to avoid using reflection on every call.
        /// </summary>
        internal sealed class PropertyMapper
        {
            /// <summary>
            /// Cache for all enums which reporesnt error codes and have custom flags.
            /// </summary>
            readonly IDictionary<Type, IDictionary<string, IDictionary<Type, object>>> _cache = new Dictionary<Type, IDictionary<string, IDictionary<Type, object>>>();

            /// <summary>
            /// Thread safe operation on cache.
            /// </summary>
            readonly object _lock = new object();

            /// <summary>
            /// Creates metadata for a given object type.
            /// </summary>
            /// <typeparam name="TType">Object type.</typeparam>
            /// <param name="flags">Attributes on fields to map.</param>
            public void Allocate<TType>(params Type[] flags)
                where TType : Enum
            {
                // Lock the entire operation to ensure fixed values.
                lock (_lock)
                {
                    // Cache the Enum type
                    var type = typeof(TType);

                    // Since there is no constrain, collection must be filtered.
                    var attributeTypes = Filter(flags);

                    // Defaults
                    var local = new Dictionary<string, IDictionary<Type, object>>();

                    // Searching for any field decorated with given flags
                    var fields = GetFields<TType>(attributeTypes);

                    if (fields?.Any() != true)
                        return; // Nothing to cache

                    // Collecting flags for each field in the enum
                    foreach (var field in fields)
                    {
                        // Default
                        var types = new Dictionary<Type, object>();

                        // Getting only the given flags
                        foreach (var item in attributeTypes)
                        {
                            var attribute = field.GetCustomAttributes(item, false)?.FirstOrDefault();

                            // Because initially we ensure field only for one attribute, this has to be checked for each of them.
                            if (attribute is null)
                                continue;

                            // Creating a collection of found types for the field
                            types.Add(item, attribute);
                        }

                        // Adding the collection to the type entry in the local dictionary.
                        local[field.Name] = types;
                    }

                    // Adds or updates found items in cache.
                    _cache[type] = local;
                }
            }

            /// <summary>
            /// Gets all fields which have defined specified flags.
            /// </summary>
            /// <typeparam name="TEnum">Enum type</typeparam>
            /// <param name="filters">Collection of flags.</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Readability", "RCS1018:Add accessibility modifiers.", Justification = "<Pending>")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerable<FieldInfo> GetFields<TEnum>(IEnumerable<Type> filters)
                => typeof(TEnum).GetFields().Where(field =>
                {
                    foreach (var item in filters)
                    {
                        if (field.IsDefined(item, false))
                            return true; // This means we have a match
                    }

                    // No match found, which means the filed has none of the specified attributed defined.
                    return false;
                });

            /// <summary>
            /// Filter collection to ensure only types inherit <see cref="Attribute"/>
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Readability", "RCS1018:Add accessibility modifiers.", Justification = "<Pending>")]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerable<Type> Filter(IEnumerable<Type> types)
                => types.Where(type => type.IsSubclassOf(typeof(Attribute)));

            /// <summary>
            /// Remove all cached entries for given type.
            /// </summary>
            /// <typeparam name="TType">Enum type.</typeparam>
            public void Clear<TType>()
                where TType : Enum
            {
                lock (_lock)
                {
                    var type = typeof(TType);
                    if (_cache.ContainsKey(type))
                        _cache.Remove(type);
                }
            }

            /// <summary>
            /// Gets the attribute from cache.
            /// </summary>
            /// <typeparam name="TReturn">Return type</typeparam>
            /// <typeparam name="TAttribute">Attribute type</typeparam>
            /// <param name="value">Enum value</param>
            public TReturn Get<TReturn, TAttribute>(Enum value, Func<TAttribute, TReturn> expression)
                where TAttribute : Attribute
            {
                var type = value.GetType();
                var typeAttribute = typeof(TAttribute);
                TAttribute found = null;

                if (_cache.ContainsKey(type))
                {
                    found = _cache[type]
                     ?.FirstOrDefault(k => k.Key == value.ToString()).Value
                     ?.FirstOrDefault(k => k.Key == typeAttribute).Value as TAttribute;
                }

                return found is null ? throw new NotSupportedException($"{typeof(TAttribute)} couldn't be located in cache!") : expression(found);
            }
        }
    }
}
