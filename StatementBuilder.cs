using System.Text;
using Microsoft.Data.SqlClient;

public static class StatementBuilder
{

    public static IEnumerable<ColumnInfo> GetColumnInfo(string table, SqlConnection connection)
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

    public static string GetSelectStatement(string table, IEnumerable<ColumnInfo> columnInfos, SqlConnection connection)
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

    public static IEnumerable<string> GetInsertStatements(string table, string selectStatement, IEnumerable<ColumnInfo> columnInfos, SqlConnection connection)
    {
        using SqlCommand cmd = new(selectStatement, connection);
        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
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
                int columnIndex = reader.GetOrdinal(columnInfo.ColumnName);
                if (reader.IsDBNull(columnIndex))
                {
                    if (!columnInfo.IsNullable)
                    {
                        throw new InvalidOperationException("Null value found in non-nullable column");
                    }
                    sb.Append("NULL");
                }
                else
                {
                    object value = reader[columnInfo.ColumnName];
                    if (columnInfo.DataType == typeof(string))
                    {
                        sb.Append("'");
                        sb.Append(((string)value).Replace("'", "''"));
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

}