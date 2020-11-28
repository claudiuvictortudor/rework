using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutomaticWip.Contracts
{
    public static class TypeMap
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static readonly ITypeMap Instance = new TypeMapImpl(typeof(TypeMapImpl));

        /// <summary>
        /// Member flags
        /// </summary>
        static readonly BindingFlags FLAGS = BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.NonPublic
            | BindingFlags.GetProperty
            | BindingFlags.GetField;

        /// <summary>
        /// Generic definition for a typeMap
        /// </summary>
        public interface ITypeMap
        {
            /// <summary>
            /// Resolves a flag from cache for a given type
            /// </summary>
            /// <typeparam name="TType">Requested type</typeparam>
            /// <typeparam name="TFlag">The attribute to look for</typeparam>
            /// <param name="name">The of the member</param>
            /// <returns>The flag if is found or null.</returns>
            TFlag Resolve<TType, TFlag>(string name)
                where TFlag : Attribute;

            /// <summary>
            /// Register all member of a type marked with given flags
            /// </summary>
            /// <typeparam name="TType">Requested type to register</typeparam>
            void Register<TType>();
        }

        /// <summary>
        /// Implementation of <see cref="ITypeMap"/>
        /// </summary>
        sealed class TypeMapImpl : ITypeMap
        {
            /// <summary>
            /// Cache for all members of a type
            /// </summary>
            readonly IDictionary<Type, IDictionary<string, IDictionary<Type, object>>> _cache;

            /// <summary>
            /// Thread safe operation on cache
            /// </summary>
            readonly object _lock;

            /// <summary>
            /// Initialize a new <see cref="TypeMapImpl"/> 
            /// </summary>
            internal TypeMapImpl(params Type[] types)
            {
                _cache = new Dictionary<Type, IDictionary<string, IDictionary<Type, object>>>();
                _lock = new object();

                foreach (var item in types?.Any() is true ? types : Array.Empty<Type>())
                    Register(item);
            }

            /// <summary>
            /// Creates metadata for a given type.
            /// </summary>
            void Register(Type type)
            {
                lock (_lock)
                {
                    if (_cache.ContainsKey(type))
                        return;

                    // Defaults
                    var local = new Dictionary<string, IDictionary<Type, object>>();

                    // Getting all the members of the requested type
                    var members = type.GetMembers(FLAGS).Where(member => member.CustomAttributes.Count() > 0);

                    // Start caching members
                    foreach (var member in members?.Any() is true ? members : Array.Empty<MemberInfo>())
                    {
                        // Default
                        var types = new Dictionary<Type, object>();

                        // All flags declared
                        var flags = member.CustomAttributes?.Select(declared => declared.AttributeType) ?? Array.Empty<Type>();

                        // Getting only the given flags
                        foreach (var item in flags)
                        {
                            // Cache the flag
                            types[item] = member.GetCustomAttributes(item).First();
                        }

                        // Adding the collection to the type entry in the local dictionary.
                        local[member.Name] = types;
                    }

                    // Adds or updates found items in cache.
                    _cache[type] = local;
                }
            }

            /// <summary>
            /// Creates metadata for a given type.
            /// </summary>
            /// <typeparam name="TType">Requested type to register</typeparam>
            public void Register<TType>()
                => Register(typeof(TType));

            /// <summary>
            /// Gets the attribute from cache.
            /// </summary>
            /// <typeparam name="TType">Requested type</typeparam>
            /// <typeparam name="TFlag">Attribute type</typeparam>
            /// <param name="name">Member's name</param>
            public TFlag Resolve<TType, TFlag>(string name)
                where TFlag : Attribute
            {
                lock (_lock)
                {
                    var type = typeof(TType);
                    var attribute = typeof(TFlag);
                    if (_cache.ContainsKey(type))
                    {
                        var value = _cache[type]
                         ?.FirstOrDefault(k => k.Key == name).Value
                         ?.FirstOrDefault(k => k.Key == attribute).Value;

                        return value is null ? null : value as TFlag;
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// Resolves a flag from cache for a given type
        /// </summary>
        /// <typeparam name="TRequested">Requested type</typeparam>
        /// <typeparam name="TFlag">The attribute to look for</typeparam>
        /// <param name="name">The of the member</param>
        /// <returns>The flag if is found or null.</returns>
        public static TFlag Resolve<TRequested, TFlag>(this ITypeMap map, string name)
            where TFlag : Attribute
            => map.Resolve<TRequested, TFlag>(name);

        /// <summary>
        /// Register all member of a type marked with given flags
        /// </summary>
        /// <param name="type">Requested type</param>
        public static void Register(this ITypeMap map, Type type)
            => map.Register(type);

        /// <summary>
        /// Register all member of a type marked with given flags
        /// </summary>
        /// <typeparam name="TType">Requested type</typeparam>
        public static void Register<TType>(this ITypeMap map)
            => map.Register(typeof(TType));

        /// <summary>
        /// Creates a <see cref="ITypeMap"/> and looks for types marked with <see cref="CacheFlag"/>
        /// </summary>
        /// <param name="assemblies">Collection of assemblies to look for</param>
        public static ITypeMap CreateMap(params Assembly[] assemblies)
            => new TypeMapImpl(assemblies?.SelectMany(assy => assy.GetTypes())?.ToArray() ?? Array.Empty<Type>());

        /// <summary>
        /// Create a <see cref="ITypeMap"/> from a collection of types
        /// </summary>
        /// <param name="types">The assembly to cache types</param>
        public static ITypeMap CreateMap(params Type[] types)
            => new TypeMapImpl(types);

        /// <summary>
        /// Gets the attribute from cache.
        /// </summary>
        /// <typeparam name="TType">Requested type</typeparam>
        /// <typeparam name="TFlag">Attribute type</typeparam>
        /// <param name="field">Member's name</param>
        public static TFlag Resolve<TType, TFlag>(this ITypeMap map, Enum field)
            where TFlag : Attribute
            => map.Resolve<TType, TFlag>(field.ToString());

        /// <summary>
        /// Resolves the member from the attribute
        /// </summary>
        /// <typeparam name="TFlag">Type of the attribute to search on</typeparam>
        /// <typeparam name="TRequestedType">Return type</typeparam>
        /// <param name="name">The name of the memeber to look for</param>
        /// <param name="resolver">How to resolve the desired type</param>
        /// <returns>The instance of the requested type</returns>
        public static TRequestedType Resolve<TFlag, TRequestedType, TType>(this ITypeMap map, string name, Func<TFlag, TRequestedType> resolver)
            where TFlag : Attribute
        {
            var flag = map.Resolve<TType, TFlag>(name);
            return flag is null ? default : resolver.Invoke(flag);
        }

        /// <summary>
        /// Resolves the member from the attribute
        /// </summary>
        /// <typeparam name="TFlag">Type of the attribute to search on</typeparam>
        /// <typeparam name="TRequestedType">Return type</typeparam>
        /// <param name="field">The name of the memeber to look for</param>
        /// <param name="resolver">How to resolve the desired type</param>
        /// <returns>The instance of the requested type</returns>
        public static TRequestedType Resolve<TFlag, TRequestedType, TType>(this ITypeMap map, Enum field, Func<TFlag, TRequestedType> resolver)
            where TFlag : Attribute
        {
            var flag = map.Resolve<TType, TFlag>(field);
            return flag is null ? default : resolver.Invoke(flag);
        }
    }
}
