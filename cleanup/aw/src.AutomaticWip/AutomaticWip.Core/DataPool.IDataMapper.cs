using System;

namespace AutomaticWip.Core
{
    public static partial class DataPool
    {
        /// <summary>
        /// Marker interface to expose different implementations.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "<Pending>")]
        public interface IDataMapper
        {
            /// <summary>
            /// Gets the attribute from cache.
            /// </summary>
            /// <typeparam name="TReturn">Return type.</typeparam>
            /// <typeparam name="TAttribute">Attribute type</typeparam>
            /// <param name="value">Enum value</param>
            TReturn Get<TReturn, TAttribute>(Enum value, Func<TAttribute, TReturn> expression)
                where TAttribute : Attribute;

            /// <summary>
            /// Creates metadata for a given object type.
            /// </summary>
            /// <typeparam name="TType">Enum type.</typeparam>
            /// <param name="flags">Attributes on fields to map.</param>
            void Allocate<TType>(params Type[] flags)
                where TType : Enum;

            /// <summary>
            /// Remove all cached entries for given type.
            /// </summary>
            /// <typeparam name="TType">Enum type.</typeparam>
            void Clear<TType>()
                where TType : Enum;
        }
    }
}
