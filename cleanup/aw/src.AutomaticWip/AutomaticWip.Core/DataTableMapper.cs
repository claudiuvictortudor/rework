using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AutomaticWip.Core
{
    /// <summary>
    /// Maps a collection of models toa datatable.
    /// </summary>
    public sealed class DataTableMapper
    {
        /// <summary>
        /// Cache for properties from a type.
        /// </summary>
        static readonly IDictionary<Type, IEnumerable<PropertyInfo>> _propertyMap = new Dictionary<Type, IEnumerable<PropertyInfo>>();

        /// <summary>
        /// Gets the properties of a type from cache.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <returns>A collection of properties of given type.</returns>
        static IEnumerable<PropertyInfo> Get<T>()
            where T : class
        {
            lock (_propertyMap)
            {
                var type = typeof(T);
                if (_propertyMap.ContainsKey(type))
                    return _propertyMap[type];

                return Array.Empty<PropertyInfo>();
            }
        }

        /// <summary>
        /// Clear the cache.
        /// </summary>
        /// <param name="types">If empty, clears all.</param>
        static internal void Clear(params Type[] types)
        {
            lock (_propertyMap)
            {
                if (types?.Any() is true)
                {
                    for (int i = 0; i < types.Length; i++)
                        _propertyMap.Remove(types[i]);
                }
                else
                    _propertyMap.Clear();
            }
        }

        /// <summary>
        /// Register an object to the mapper.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        static void Register<T>()
            where T : class
        {
            var type = typeof(T);
            if (!_propertyMap.ContainsKey(type))
            {
                var properties = type.GetProperties().OrderBy(p => p.Name);
                if (properties?.Any() is true)
                    _propertyMap.Add(type, properties);
            }
        }

        /// <summary>
        /// Maps a collection of models to a datatable.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <param name="values">Objects</param>
        /// <param name="tableName">Name of the datatable.</param>
        static public DataTable GetTable<T>(IEnumerable<T> values, string tableName = "")
            where T : class
        {
            lock (_propertyMap)
            {
                //Defaults
                var table = string.IsNullOrWhiteSpace(tableName)
                    ? new DataTable()
                    : new DataTable(tableName);

                var type = typeof(T);

                // Ensure this is mapped.
                Register<T>();

                // The list of properties already sorted.
                var properties = _propertyMap[type];

                // Adding all the columns
                foreach (var item in properties)
                    table.Columns.Add(item.Name, item.PropertyType);

                // Adding all the rows
                foreach (var item in values)
                {
                    object[] propertyValues = properties.Select(p => p.GetValue(item)).ToArray();
                    table.Rows.Add(propertyValues);
                }

                return table;
            }
        }
    }
}
