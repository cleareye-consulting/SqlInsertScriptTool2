using System.Text;
using Microsoft.Data.SqlClient;

public abstract class StatementBuilder : IDisposable
{

    protected SqlConnection connection;

    public StatementBuilder(SqlConnection connection)
    {
        this.connection = connection;
    }

    public static StatementBuilder GetInstance(string sourceMode, SqlConnection connection)
    {
        if (sourceMode.Equals("DB", StringComparison.CurrentCultureIgnoreCase))
        {
            return new DbSourcedStatementBuilder(connection);
        }
        if (sourceMode.Equals("CSV", StringComparison.CurrentCultureIgnoreCase))
        {
            return new CsvSourcedStatementBuilder(connection);
        }
        throw new ArgumentException("Source mode should be 'DB' or 'CSV'");
    }

    //This is a hack to solve a very specific problem, could probably be generalized to the parameters
    public readonly HashSet<(string, string)> sensitiveColumns = new HashSet<(string, string)>{
        ("CoatingComponent", "CoatingComponentDescription")
    };

    public IEnumerable<ColumnInfo> GetColumnInfo(string table)
    {

        string sql = @"
            select COLUMN_NAME, DATA_TYPE, IS_NULLABLE
            from INFORMATION_SCHEMA.COLUMNS 
            where TABLE_NAME = @tableName 
            order by ORDINAL_POSITION";
        using SqlCommand cmd = new(sql, connection);
        SqlParameter tableNameParam = cmd.Parameters.Add("tableName", System.Data.SqlDbType.NVarChar, 128);
        tableNameParam.Value = table;
        using SqlDataReader reader = cmd.ExecuteReader();
        List<ColumnInfo> results = new();
        while (reader.Read())
        {
            ColumnInfo result = new()
            {
                ColumnName = (string)reader["COLUMN_NAME"],
                DataTypeName = (string)reader["DATA_TYPE"],
                IsNullable = (string)reader["IS_NULLABLE"] == "YES"
            };
            results.Add(result);
        }
        return results;
    }

    public abstract void Initialize(string tableName, IEnumerable<ColumnInfo> columnInfos);
    public abstract bool GetNext();
    public abstract bool IsNull(string columnName);
    public abstract object GetValue(string columnName);

    public IEnumerable<string> GetInsertStatements(string table, IEnumerable<ColumnInfo> columnInfos)
    {
        Initialize(table, columnInfos);
        while (GetNext())
        {
            StringBuilder sb = new();
            sb.Append("insert ");
            sb.Append(table);
            sb.Append(" (");
            bool first = true;
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
            sb.Append(") values (");
            first = true;
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
                if (IsNull(columnInfo.ColumnName))
                {
                    if (!columnInfo.IsNullable)
                    {
                        throw new InvalidOperationException("Null value found in non-nullable column");
                    }
                    sb.Append("NULL");
                }
                else
                {
                    //object value = reader[columnInfo.ColumnName];
                    object value = GetValue(columnInfo.ColumnName);
                    if (columnInfo.DataType == typeof(string))
                    {
                        sb.Append("'");
                        if (sensitiveColumns.Contains((table, columnInfo.ColumnName)))
                        {
                            sb.Append("OBFUSCATED");
                        }
                        else
                        {
                            sb.Append(((string)value).Replace("'", "''"));
                        }
                        sb.Append("'");
                    }
                    else if (columnInfo.DataType == typeof(DateTime))
                    {
                        sb.Append("'");
                        sb.Append(((DateTime)value).ToString("s"));
                        sb.Append("'");
                    }
                    else if (columnInfo.DataType == typeof(bool))
                    {
                        sb.Append((bool)value ? '1' : '0');
                    }
                    else
                    { //This could be more robust; currently it assumes it'll be a number at this point
                        sb.Append(value.ToString());
                    }
                }
            }
            sb.Append(")");
            yield return sb.ToString();
        }
    }

    public abstract void Dispose();

}