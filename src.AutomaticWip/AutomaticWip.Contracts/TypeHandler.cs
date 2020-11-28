using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomaticWip.Contracts
{
    public static class TypeHandler
    {
        /// <summary>
        /// Converts an object to a string
        /// </summary>
        public sealed class StringToStringConverter : ITypeHandler
        {
            public object Request(object value)
            {
                Compile.Against<ArgumentNullException>(value is null, "Value cannot be null!");
                return value as string;
            }
        }

        /// <summary>
        /// Converts an object to a string
        /// </summary>
        public sealed class StringToCharConverter : ITypeHandler
        {
            public object Request(object value)
            {
                Compile.Against<ArgumentNullException>(value is null, "Value cannot be null!");
                return (value as string)[0];
            }
        }

        /// <summary>
        /// Converts an object to <see cref="bool"/>
        /// </summary>
        public sealed class StringToBoolConverter : ITypeHandler
        {
            /// <summary>
            /// A local convertor for string
            /// </summary>
            readonly ITypeHandler STRING_HANDLER = new StringToStringConverter();

            /// <summary>
            /// Handle bool cases
            /// </summary>
            /// <param name="value">Input value from config</param>
            /// <returns>Parsed bool</returns>
            bool ConvertToBool(string value)
            {
                switch (value.ToUpper())
                {
                    case "TRUE":
                        return true;
                    case "FALSE":
                        return false;
                    case "1":
                        return true;
                    case "0":
                        return false;
                    case "YES":
                        return true;
                    case "NO":
                        return false;
                    case "Y":
                        return true;
                    case "N":
                        return false;
                    case "P":
                        return true;
                    case "F":
                        return false;
                    default:
                        throw new InvalidCastException($"value '{value}' cannot be parsed as bool!");
                }
            }

            /// <summary>
            /// Boolean conversion
            /// </summary>
            /// <param name="value">The object's value</param>
            /// <returns>Converted object</returns>
            public object Request(object value)
            {
                var parsed = STRING_HANDLER.Request(value);
                var local = parsed is null ? "" : parsed as string;
                Compile.Against<NotSupportedException>(local.Length < 1, "Cannot convert an empty string to bool!");
                return ConvertToBool(local);
            }
        }

        /// <summary>
        /// Converts an object to a numeric value
        /// </summary>
        /// <typeparam name="T">Numeric type</typeparam>
        public sealed class StringToNumericConverter<T> : ITypeHandler
            where T : struct
        {
            /// <summary>
            /// A local convertor for string
            /// </summary>
            readonly ITypeHandler STRING_HANDLER = new StringToStringConverter();

            /// <summary>
            /// Checks any number type
            /// </summary>
            readonly Regex _isNumericRegex = new Regex("^(" +
                    /*Hex*/ @"0x[0-9a-f]+" + "|" +
                    /*Bin*/ @"0b[01]+" + "|" +
                    /*Oct*/ @"0[0-7]*" + "|" +
                    /*Dec*/ @"((?!0)|[-+]|(?=0+\.))(\d*\.)?\d+(e\d+)?" +
                    ")$");

            public object Request(object value)
            {
                var parsed = STRING_HANDLER.Request(value);
                var local = parsed is null ? value.ToString() : parsed as string;
                Compile.Against<NotSupportedException>(local.Length < 1, "Cannot convert an empty string to numeric!");
                Compile.Against<InvalidCastException>(!_isNumericRegex.IsMatch(local), $"value '{value}' cannot be parsed as numeric!");

                return Convert(local);
            }

            /// <summary>
            /// Custom numeric
            /// </summary>
            /// <param name="value">The value to convert</param>
            object Convert(string value)
            {
                switch (typeof(T))
                {
                    case Type shortType when shortType == typeof(short):
                        return short.Parse(value);
                    case Type ushortType when ushortType == typeof(ushort):
                        return ushort.Parse(value);
                    case Type intType when intType == typeof(int):
                        return int.Parse(value);
                    case Type uintType when uintType == typeof(uint):
                        return uint.Parse(value);
                    case Type longType when longType == typeof(long):
                        return long.Parse(value);
                    case Type ulongType when ulongType == typeof(ulong):
                        return ulong.Parse(value);
                    case Type doubleType when doubleType == typeof(double):
                        return double.Parse(value);
                    case Type floatType when floatType == typeof(float):
                        return float.Parse(value);
                    case Type deciType when deciType == typeof(decimal):
                        return decimal.Parse(value);
                    default:
                        throw new InvalidCastException($"{typeof(T)} is not a numeric type!");
                }
            }
        }

        /// <summary>
        /// Converts a string to an array
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        public abstract class StringToArrayConverter<T> : ITypeHandler
        {
            /// <summary>
            /// A local convertor for string
            /// </summary>
            readonly ITypeHandler STRING_HANDLER = new StringToStringConverter();

            public object Request(object value)
            {
                var def = new List<T>();
                var parsed = STRING_HANDLER.Request(value);
                var local = parsed is null ? "" : parsed as string;
                foreach (var item in Split(local))
                    def.Add(Create(item));

                return def.ToArray();
            }

            /// <summary>
            /// Splits the input
            /// </summary>
            /// <param name="value">The value to split</param>
            protected abstract string[] Split(string value);

            /// <summary>
            /// Create the object from string
            /// </summary>
            protected abstract T Create(string value);
        }

        /// <summary>
        /// Converts an string to string[]
        /// </summary>
        public sealed class StringToStringArrayConverter : StringToArrayConverter<string>
        {
            readonly char _splitter;

            public StringToStringArrayConverter(char splitter)
            {
                _splitter = splitter;
            }

            protected override string Create(string value)
                => value;

            protected override string[] Split(string value)
                => value.Contains(_splitter) ? value.Split(_splitter) : new string[] { value };
        }

        /// <summary>
        /// Converts an string to string[]
        /// </summary>
        public sealed class StringToNumericArrayConverter<T> : StringToArrayConverter<T>
            where T : struct
        {
            /// <summary>
            /// Deleimiter char
            /// </summary>
            readonly char _splitter;

            /// <summary>
            /// A local convertor for string
            /// </summary>
            readonly ITypeHandler NUMERIC_HANDLER = new StringToNumericConverter<T>();

            /// <summary>
            /// Initialzie a new <see cref="StringToNumericArrayConvertor{T}"/>
            /// </summary>
            public StringToNumericArrayConverter(char splitter)
                => _splitter = splitter;

            protected override T Create(string value)
                => (T)NUMERIC_HANDLER.Request(value);

            protected override string[] Split(string value)
                => value.Contains(_splitter) ? value.Split(_splitter) : new string[] { value };
        }

        /// <summary>
        /// Parse a string to bit array
        /// </summary>
        public sealed class StringToBitArrayConverter : StringToArrayConverter<KeyValuePair<uint, bool>>
        {
            /// <summary>
            /// The delimiter for bit value
            /// </summary>
            readonly char _valDelimiter;

            /// <summary>
            /// Split the input to bits
            /// </summary>
            readonly ITypeHandler STRING_ARR_HANDLER;

            /// <summary>
            /// Boolean convertor
            /// </summary>
            readonly ITypeHandler BOOL_HANDLER = new StringToBoolConverter();

            /// <summary>
            /// Numeric convertor
            /// </summary>
            readonly ITypeHandler NUMERIC_HANDLER = new StringToNumericConverter<uint>();

            /// <summary>
            /// Initialize a new <see cref="StringToBitArrayConvertor"/>
            /// </summary>
            public StringToBitArrayConverter(char valDelimiter, char bitDelimiter)
            {
                STRING_ARR_HANDLER = new StringToStringArrayConverter(bitDelimiter);
                _valDelimiter = valDelimiter;
            }

            protected override KeyValuePair<uint, bool> Create(string value)
            {
                var split = value.Split(_valDelimiter);
                Compile.Against<NotSupportedException>(split.Length != 2, $"Invalid position/value: '{value}'");
                var pos = (uint)NUMERIC_HANDLER.Request(split[0]);
                var val = (bool)BOOL_HANDLER.Request(split[1]);
                return new KeyValuePair<uint, bool>(pos, val);
            }

            protected override string[] Split(string value)
                => (string[])STRING_ARR_HANDLER.Request(value);
        }

        /// <summary>
        /// Converts a string to a byte
        /// 0:0;1:y;6:no
        /// </summary>
        public sealed class StringToByteConverter : ITypeHandler
        {
            /// <summary>
            /// Bit pairs convertor
            /// </summary>
            readonly ITypeHandler BIT_ARR_HANDLER;

            /// <summary>
            /// Initialize a new <see cref="StringToByteConverter"/>
            /// </summary>
            public StringToByteConverter(char valDelimiter, char bitDelimiter)
                => BIT_ARR_HANDLER = new StringToBitArrayConverter(valDelimiter, bitDelimiter);

            public object Request(object value)
            {
                // Splitted byte with 0 on each position
                var bits = new BitArray(8);

                // Output value
                byte[] local = new byte[10];

                // Split the input in pairs(pos:val)
                var parsedBits = (KeyValuePair<uint, bool>[])BIT_ARR_HANDLER.Request(value);

                // Validations
                Compile.Against<NotSupportedException>(parsedBits.Length == 0, "Invalid input: bit count < 1");
                Compile.Against<NotSupportedException>(parsedBits.Length > 8, "Invalid input: bit count > 8");

                // Override the positions from the input
                foreach (var item in parsedBits)
                {
                    Compile.Against<NotSupportedException>(item.Key > 7, $"Invalid bit position: bit position > {item.Key}");
                    bits.Set((int)item.Key, item.Value);
                }

                // Copy the bits to the byte[]
                bits.CopyTo(local, 0);

                // Since only 8 bits get written, only the first byte is populated
                return local[0];
            }
        }
    }
}
