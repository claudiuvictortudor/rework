using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace AutomaticWip.Contracts
{
    public static class DataTableExtensions
    {
        /// <summary>
        /// Cache
        /// </summary>
        static readonly IDictionary<Type, IEnumerable<Property>> _map;

        /// <summary>
        /// Property options
        /// </summary>
        sealed class Property
        {
            internal string Alias;
            internal Type PropertyType;
            internal PropertyInfo Info;
        }

        /// <summary>
        /// Thread synchronization
        /// </summary>
        static readonly object _lock;

        /// <summary>
        /// Mapping value
        /// </summary>
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
        public sealed class MapTo : Attribute
        {
            /// <summary>
            /// The value to map to
            /// </summary>
            public string Value { get; set; }
        }

        /// <summary>
        /// Flag for registering models
        /// </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class DataModel : Attribute { }

        /// <summary>
        /// Auto registrations
        /// </summary>
        static DataTableExtensions()
        {
            _map = new Dictionary<Type, IEnumerable<Property>>();
            _lock = new object();
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes().Where(type => type.IsDefined(typeof(DataModel))));

            foreach (var item in types)
                Register(item);
        }

        /// <summary>
        /// Register an object to the mapper.
        /// </summary>
        /// <param name="type">Requested type.</param>
        public static void Register(Type type)
        {
            if (!_map.ContainsKey(type))
            {
                var list = new List<Property>();
                var properties = type.GetProperties().OrderBy(p => p.Name);
                if (properties?.Any() is true)
                {
                    foreach (var item in properties)
                    {
                        var prop = new Property();
                        prop.PropertyType = item.PropertyType;
                        prop.Alias = (item.GetCustomAttribute(typeof(MapTo)) as MapTo)?.Value ?? item.Name;
                        prop.Info = item;
                        list.Add(prop);
                    }
                }

                _map[type] = list;
            }
        }

        /// <summary>
        /// Create a collection of models from a datatable
        /// </summary>
        /// <typeparam name="T">Model type</typeparam>
        /// <param name="table">Data source</param>
        /// <param name="filter">Expression to filter the collection</param>
        /// <returns>A collection of models</returns>
        public static IEnumerable<T> AsEnumerable<T>(this DataTable table, string filter = null)
        {
            var type = typeof(T);
            lock (_lock)
            {
                Compile.Against<ArgumentNullException>(table is null, "Data source cannot be null!");
                if (!_map.ContainsKey(type))
                    Register(type);

                var columns = table.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                var cached = _map[type];
                foreach (var row in string.IsNullOrWhiteSpace(filter) ? table.Select() : table.Select(filter))
                {
                    var instance = Activator.CreateInstance<T>();
                    foreach (var item in cached)
                    {
                        if (columns.Contains(item.Alias) && !row.IsNull(item.Alias))
                            item.Info.SetValue(instance, row[item.Alias]);
                    }

                    yield return instance;
                }
            }
        }

        /// <summary>
        /// Maps a collection of models to a data table.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <param name="values">Objects</param>
        /// <param name="tableName">Name of the data table.</param>
        static public DataTable AsTable<T>(this IEnumerable<T> values, string tableName = "")
            where T : class
        {
            //Defaults
            var table = string.IsNullOrWhiteSpace(tableName)
                ? new DataTable()
                : new DataTable(tableName);

            var type = typeof(T);

            lock (_lock)
            {
                // The list of properties already sorted.
                var properties = _map[type];

                // Adding all the columns
                foreach (var item in properties)
                    table.Columns.Add(item.Alias, item.PropertyType);

                // Adding all the rows
                foreach (var item in values)
                {
                    object[] propertyValues = properties.Select(p => p.Info.GetValue(item)).ToArray();
                    table.Rows.Add(propertyValues);
                }

                return table;
            }
        }

        /// <summary>
        /// Maps a collection of models to a datatable.
        /// </summary>
        /// <typeparam name="T">Model type.</typeparam>
        /// <param name="values">Objects</param>
        /// <param name="tableName">Name of the datatable.</param>
        static public DataTable AsTable<T>(this IEnumerable<T> values, DataTable table)
            where T : class
        {
            var type = typeof(T);
            lock (_lock)
            {
                // The list of properties already sorted.
                var properties = _map[type];
                var columns = table.Columns;

                // Adding all the rows
                foreach (var item in values)
                {
                    var local = new object[columns.Count];

                    // Adding all the rows
                    foreach (DataColumn column in columns)
                    {
                        local[column.Ordinal] = properties
                            .FirstOrDefault(p => p.Alias == column.ColumnName && p.PropertyType == column.DataType)
                            ?.Info.GetValue(item) ?? default;
                    }

                    table.Rows.Add(local);
                }

                return table;
            }
        }
    }
}
