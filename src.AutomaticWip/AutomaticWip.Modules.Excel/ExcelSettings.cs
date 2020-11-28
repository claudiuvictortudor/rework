using System;

namespace AutomaticWip.Modules.Excel
{
    /// <summary>
    /// Provide static info for rendering the file
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ExcelSettings : Attribute
    {
        /// <summary>
        /// Column type format
        /// </summary>
        public ExcelFieldType Format { get; set; } = ExcelFieldType.String;

        /// <summary>
        /// Name of the column
        /// </summary>
        public char Column { get; set; }

        /// <summary>
        /// The max size of this column
        /// </summary>
        public int Size { get; set; }
    }
}
