using System;

namespace AutomaticWip.Contracts
{
    /// <summary>
    /// Generic cache for application's settings
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// Get the config settings
        /// </summary>
        /// <typeparam name="T">The type of expected value</typeparam>
        /// <param name="section">Name of the section</param>
        /// <param name="property">Name of the property</param>
        /// <param name="value">The value of the property</param>
        /// <param name="throw">If true, when an exception occures will throw it back to caller</param>
        /// <returns>True if property is found.</returns>
        bool Get<T>(string section, string property, out T value, bool @throw = false);

        /// <summary>
        /// Adds a type handler
        /// </summary>
        /// <param name="handler">The convertor for given type</param>
        void Set(string section, Type type, ITypeHandler handler);
    }
}
