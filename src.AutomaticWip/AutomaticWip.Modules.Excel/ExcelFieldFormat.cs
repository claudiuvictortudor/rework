using System;

namespace AutomaticWip.Modules.Excel
{
    /// <summary>
    /// Maps a format to a readable field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ExcelFieldFormat : Attribute
    {
        /// <summary>
        /// The format of current cell range
        /// </summary>
        public string Format { get; set; }
    }
}
