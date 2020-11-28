using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;

namespace AutomaticWip.Contracts
{
    /// <summary>
    /// Implementation of IConfiguration
    /// </summary>
    public sealed class Settings : IConfiguration
    {
        /// <summary>
        /// Connection string to the clone server
        /// </summary>
        public const string TMAS275A = @"Server=TMAS275A.cw01.contiwan.com\I0001;Database=AutomaticWip;User Id=sa;Password=Q!w2e3r4t5;";

        /// <summary>
        /// The connection string to the oltp MES server
        /// </summary>
        public const string MES_OLTP = "Data Source=(DESCRIPTION =(ADDRESS =(PROTOCOL = TCP)(HOST = tmdb008.TM.RO.CONTI.DE)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = REPORTING.TM.RO.CONTI.DE)));User Id=MESREAD;Password=MESREAD;";

        /// <summary>
        /// The connection string to the olap DWH MES server
        /// </summary>
        public const string MES_OLAP = "Data Source=(DESCRIPTION =(ADDRESS =(PROTOCOL = TCP)(HOST = tmdb003.cw01.contiwan.com)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = MESTMDWH.cw01.contiwan.com)));User Id=MDICE_REPORTS;Password=MDICE_REPORTS;";

        /// <summary>
        /// app.config handler
        /// </summary>
        public static readonly IConfiguration Default = new Settings();

        /// <summary>
        /// Ensure thread synchronisation
        /// </summary>
        readonly object _lock = new object();

        /// <summary>
        /// Read error details
        /// </summary>
        const string ERROR_TEMPLATE = "Configuration.Resolve('{0}', '{1}', out {3} value) -> {2}";

        /// <summary>
        /// Cache for each section/property-value pairs
        /// </summary>
        readonly IDictionary<string, IDictionary<string, object>> _cache = new Dictionary<string, IDictionary<string, object>>();

        /// <summary>
        /// Type handler map
        /// </summary>
        readonly IDictionary<string, IDictionary<Type, ITypeHandler>> _typeHandler = new Dictionary<string, IDictionary<Type, ITypeHandler>>();

        /// <summary>
        /// Initialize a new <see cref="Settings"/>
        /// </summary>
        Settings()
        {
            // looking in default section
            var def = ConfigurationManager.AppSettings;
            if (def?.Count > 0)
            {
                var local = new Dictionary<string, object>();

                // cache all entries for later use
                foreach (var item in def.Keys)
                    local[item.ToString()] = def[item.ToString()];

                _cache["appSettings"] = local;
            }

            _typeHandler["appSettings"] = GetDefaultHandlers();
        }

        /// <summary>
        /// Gets default implementation of <see cref="ITypeHandler"/> for known types
        /// </summary>
        IDictionary<Type, ITypeHandler> GetDefaultHandlers()
        {
            var handlers = new Dictionary<Type, ITypeHandler>();

            // Common types
            handlers[typeof(string)] = new TypeHandler.StringToStringConverter();
            handlers[typeof(char)] = new TypeHandler.StringToCharConverter();
            handlers[typeof(string[])] = new TypeHandler.StringToStringArrayConverter(';');
            handlers[typeof(bool)] = new TypeHandler.StringToBoolConverter();
            handlers[typeof(short)] = new TypeHandler.StringToNumericConverter<short>();
            handlers[typeof(ushort)] = new TypeHandler.StringToNumericConverter<ushort>();
            handlers[typeof(int)] = new TypeHandler.StringToNumericConverter<int>();
            handlers[typeof(uint)] = new TypeHandler.StringToNumericConverter<uint>();
            handlers[typeof(long)] = new TypeHandler.StringToNumericConverter<long>();
            handlers[typeof(ulong)] = new TypeHandler.StringToNumericConverter<ulong>();
            handlers[typeof(decimal)] = new TypeHandler.StringToNumericConverter<decimal>();
            handlers[typeof(double)] = new TypeHandler.StringToNumericConverter<double>();
            handlers[typeof(float)] = new TypeHandler.StringToNumericConverter<float>();

            return handlers;
        }

        /// <summary>
        /// Gets the section from cache or other entities
        /// </summary>
        /// <param name="section">Name of the section to look for</param>
        /// <returns>The collection of entries or null.</returns>
        IDictionary<string, object> GetSection(string section)
        {
            if (!_cache.ContainsKey(section))
            {
                // Looking for singletagconfighandler
                var configSection = (IDictionary)ConfigurationManager.GetSection(section);
                if (configSection != null && configSection.Count > 0)
                {
                    var local = new Dictionary<string, object>();

                    // cache all entries for later use
                    foreach (var item in configSection.Keys)
                        local[item.ToString()] = configSection[item];

                    _cache[section] = local;
                }
                else // not found
                    return null;
            }

            _typeHandler[section] = GetDefaultHandlers();
            return _cache[section];
        }

        /// <summary>
        /// Resolve a property from app.config
        /// </summary>
        /// <typeparam name="T">Type of the value to extract</typeparam>
        /// <param name="section">Section where this property is declared</param>
        /// <param name="property">The name of the property</param>
        /// <param name="value">The value of the property</param>
        /// <returns>True if property is found</returns>
        public bool Get<T>(string section, string property, out T value, bool @throw = false)
        {
            value = default;
            var type = typeof(T);

            try
            {
                // Validations on input
                Compile.Against<ArgumentException>(
                    string.IsNullOrWhiteSpace(section),
                    string.Format(ERROR_TEMPLATE, section, property, $"{nameof(section)} cannot be null/empty!", type.Name),
                    () => section = /* !null */ "$");

                Compile.Against<ArgumentException>(
                    string.IsNullOrWhiteSpace(property),
                    string.Format(ERROR_TEMPLATE, section, property, $"{nameof(property)} cannot be null/empty!", type.Name),
                    () => property = /* !null */ "$");


                lock (_lock) // --|--|--
                {
                    // Try to resolve it from cache, or cache it 
                    var configSection = GetSection(section);

                    // If section is not found, exit
                    Compile.Against<NotSupportedException>(
                        configSection is null,
                        string.Format(ERROR_TEMPLATE, section, property, $"section '{section}' not found!", type.Name));

                    // If property is not found, exit
                    Compile.Against<NotSupportedException>(
                        !configSection.ContainsKey(property),
                        string.Format(ERROR_TEMPLATE, section, property, $"property '{property}' not found at section '{section}'!", type.Name));

                    value = (T)_typeHandler[section][type].Request(configSection[property]);
                }

                return true;
            }
            catch (Exception)
            {
                if (@throw)
                    throw;

                return false;
            }
        }

        /// <summary>
        /// Register a type handler to the collection
        /// </summary>
        /// <param name="handler">A resolver for the requested type</param>
        public void Set(string section, Type type, ITypeHandler handler)
        {
            Compile.Against<ArgumentNullException>(handler is null, "Cannot register null handlers!");

            lock (_lock)
            {
                _typeHandler[section][type] = handler;
            }
        }
    }
}
