using AutomaticWip.Contracts;
using AutomaticWip.Modules.Excel;
using AutomaticWip.Modules.Stocks.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AutomaticWip.Modules.Stocks.ExcelSerializers
{
    public sealed class AggregateSerializer : ExcelSerializer<StockAggregation>
    {
        readonly IDictionary<Type, ITypeHandler> TypeHandler = new Dictionary<Type, ITypeHandler>();

        public AggregateSerializer()
            :base()
        {
            TypeMap.Register<StockAggregation>();

            TypeHandler[typeof(string)] = new TypeHandler.StringToStringConverter();
            TypeHandler[typeof(double)] = new TypeHandler.StringToNumericConverter<double>();

            Settings[nameof(StockAggregation.Area)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Area));
            Settings[nameof(StockAggregation.BetweenTwoAndFiveDays)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.BetweenTwoAndFiveDays));
            Settings[nameof(StockAggregation.Description)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Description));
            Settings[nameof(StockAggregation.Division)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Division));
            Settings[nameof(StockAggregation.Material)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Material));
            Settings[nameof(StockAggregation.MoreThanFiveDays)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.MoreThanFiveDays));
            Settings[nameof(StockAggregation.Project)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Project));
            Settings[nameof(StockAggregation.SmallerOrEqualOneDay)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.SmallerOrEqualOneDay));
            Settings[nameof(StockAggregation.Zone)] = TypeMap.Resolve<StockAggregation, ExcelSettings>(nameof(StockAggregation.Zone));
        }

        protected override string Name 
            => "AGGREGATE";

        protected override string Security 
            => "AGGREGATE";

        protected override void OnFormat(char column, ExcelRange cells, uint head, uint tail)
        {
            switch (column)
            {
                // Area column
                // If PN doesn;t contains area, alert user by making text red
                case 'F':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out string area);
                        if (area?.IndexOf("N/A", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cell.Style.Font.Color.SetColor(Color.IndianRed);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;

                // Division area
                // If PN doesn;t contains division, alert user by making text red
                case 'D':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out string division);
                        if (division?.IndexOf("N/A", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cell.Style.Font.Color.SetColor(Color.IndianRed);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;

                // Zone area
                // If PN doesn;t contains zone, alert user by making text red
                case 'E':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out string zone);
                        if (zone?.IndexOf("N/A", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cell.Style.Font.Color.SetColor(Color.IndianRed);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;

                // <2Days
                // If value > 0 make it green text
                case 'G':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out double value);
                        if (value > 0)
                        {
                            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(1, 51, 204, 51);
                            cell.Style.Font.Color.SetColor(Color.Black);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;

                // >= 2 DAYS & <= 5 DAYS
                // If value > 0 make it orange text
                case 'H':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out double value);
                        if (value > 0)
                        {
                            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.Orange);
                            cell.Style.Font.Color.SetColor(Color.Black);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;

                // > 5 DAYS
                // If value > 0 make it red text
                case 'I':
                    for (uint i = head; i < tail + 1; i++)
                    {
                        var cell = Extract(cells, column, i, out double value);
                        if (value > 0)
                        {
                            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.Red);
                            cell.Style.Font.Color.SetColor(Color.Black);
                            cell.Style.Font.Bold = true;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Extract the cell from the range
        /// </summary>
        /// <param name="range">Full range of cells</param>
        /// <param name="column">Column letter</param>
        /// <param name="iteration">Row number</param>
        /// <param name="value">Extracted value</param>
        /// <returns>The cell index</returns>
        ExcelRange Extract<T>(ExcelRange range, char column, uint iteration, out T value)
        {
            var cell = range[$"{column}{iteration}"];
            value = (T)TypeHandler[typeof(T)].Request(cell.Value);
            return cell;
        }
    }
}
