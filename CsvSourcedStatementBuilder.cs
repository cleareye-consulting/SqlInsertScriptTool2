using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

public class CsvSourcedStatementBuilder : StatementBuilder
{
    private TextReader? reader;
    private IDictionary<string, ColumnInfo>? columnInfoTable;
    private IDictionary<string, int>? csvColumnIndices;
    private string[]? currentRowValues;
    private bool rereadNeeded = false;

    public CsvSourcedStatementBuilder(SqlConnection connection) : base(connection) { }

    public string? CSVDirectoryPath { get; set; }

    private static string[]? GetStringValues(TextReader reader)
    {
        string? line = reader.ReadLine();
        if (line is null)
        {
            return null;
        }
        return Utilities.SplitCSVLine(line).ToArray();
    }

    public override void Initialize(string tableName, IEnumerable<ColumnInfo> columnInfos)
    {
        if (CSVDirectoryPath is null)
        {
            throw new InvalidOperationException("CSVDirectoryPath not set");
        }
        columnInfoTable = columnInfos.ToDictionary(ci => ci.ColumnName);
        string statement = GetSelectStatement(tableName, columnInfos);
        reader = new StreamReader(Path.Combine(CSVDirectoryPath, $"{tableName}.csv"));
        string[]? firstLineValues = GetStringValues(reader);
        if (firstLineValues is null)
        {
            throw new InvalidOperationException("Unable to read first line from file");
        }
        csvColumnIndices = new Dictionary<string, int>();
        if (firstLineValues.All(v => columnInfos.Any(ci => ci.ColumnName == v)))
        {
            //The CSV has the column headers in the first line
            for (int i = 0; i < firstLineValues.Length; i++)
            {
                csvColumnIndices[firstLineValues[i]] = i;
            }
        }
        else
        {
            //The first line is data
            int i = 0;
            currentRowValues = firstLineValues;
            rereadNeeded = true;
            foreach (ColumnInfo columnInfo in columnInfos)
            {
                csvColumnIndices[columnInfo.ColumnName] = i++;
            }
        }

    }

    public override bool GetNext()
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        if (rereadNeeded)
        { //this is just so that we can serve the first line when the first line is data and not headers
            rereadNeeded = false;
        }
        else
        {
            currentRowValues = GetStringValues(reader);
        }
        return currentRowValues is not null;
    }

    public override bool IsNull(string columnName)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        if (currentRowValues is null)
        {
            throw new InvalidOperationException("No current row loaded");
        }
        if (csvColumnIndices is null)
        {
            throw new InvalidOperationException("CSV column indices not loaded");
        }
        string stringValue = currentRowValues[csvColumnIndices[columnName]];
        return stringValue == "NULL"; //Assumes that the data was exported from SSMS or similar
    }

    public override object GetValue(string columnName)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        if (currentRowValues is null)
        {
            throw new InvalidOperationException("No current row loaded");
        }
        if (csvColumnIndices is null)
        {
            throw new InvalidOperationException("CSV column indices not loaded");
        }
        if (columnInfoTable is null)
        {
            throw new InvalidOperationException("Column info not loaded");
        }
        string stringValue = currentRowValues[csvColumnIndices[columnName]];
        ColumnInfo columnInfo = columnInfoTable[columnName];
        return columnInfo.DataTypeName switch
        {
            "bigint" => long.Parse(stringValue),
            "bit" => stringValue == "1",
            "decimal" or "money" or "numeric" or "smallmoney" => decimal.Parse(stringValue),
            "int" => int.Parse(stringValue),
            "smallint" => short.Parse(stringValue),
            "tinyint" => byte.Parse(stringValue),
            "float" => double.Parse(stringValue),
            "real" => typeof(float),
            "datetime" or "smalldatetime" => DateTime.ParseExact(stringValue, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.CurrentCulture),
            "datetime2" => DateTime.ParseExact(stringValue, "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.CurrentCulture),
            "date" => throw new NotSupportedException("DateOnly not supported"),
            "time" => throw new NotSupportedException("TimeOnly not supported"),
            "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => stringValue,
            _ => stringValue
        };
    }

    private string GetSelectStatement(string table, IEnumerable<ColumnInfo> columnInfos)
    {
        bool first = true;
        StringBuilder sb = new();
        sb.Append("select ");
        foreach (ColumnInfo columnInfo in columnInfos)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.Append(", ");
            }
            sb.Append(columnInfo.ColumnName);
        }
        sb.Append($" from {table}");
        return sb.ToString();
    }



    public override void Dispose()
    {
        reader?.Dispose();
        base.connection?.Dispose();
    }

}