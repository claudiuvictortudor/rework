namespace AutomaticWip.Modules.Excel
{
    /// <summary>
    /// Format options for cells in excel worksheet.
    /// </summary>
    public enum ExcelFieldType : int
    {
        String = 0,

        //integer (not really needed unless you need to round numbers, Excel will use default cell properties)
        [ExcelFieldFormat(Format = "0")]
        Integer = 1,

        //custom DateTime pattern
        [ExcelFieldFormat(Format = "yyy-MM-dd HH:mm:ss")]
        DateTime = 2,

        //number with 2 decimal places and thousand separator and money symbol
        [ExcelFieldFormat(Format = "€#,##0.00")]
        Money = 3,

        //percentage (1 = 100%, 0.01 = 1%)
        [ExcelFieldFormat(Format = "0%")]
        Percent = 4,

        //number with 2 decimal places and thousand separator
        [ExcelFieldFormat(Format = "#,##0.00")]
        Decimal = 5
    }
}
