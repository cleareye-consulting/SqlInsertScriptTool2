using System.Text;
using Microsoft.Data.SqlClient;

public class DbSourcedStatementBuilder : StatementBuilder
{

    private SqlCommand? command;
    private SqlDataReader? reader;

    public DbSourcedStatementBuilder(SqlConnection connection) : base(connection) { }

    public override void Initialize(string tableName, IEnumerable<ColumnInfo> columnInfos)
    {
        string statement = GetSelectStatement(tableName, columnInfos);
        command = new(statement, connection);
        reader = command.ExecuteReader();
    }

    public override void FinalizeTable()
    {
        command?.Dispose();
        reader?.Close();
    }

    public override bool GetNext()
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        return reader.Read();
    }

    public override bool IsNull(string columnName)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        int columnIndex = reader.GetOrdinal(columnName);
        return reader.IsDBNull(columnIndex);
    }

    public override object GetValue(string columnName)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("Reader not initialized");
        }
        return reader[columnName];
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
        reader?.Close();
        command?.Dispose();
        base.connection?.Dispose();
    }

}