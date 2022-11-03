public record ColumnInfo
{
    public string ColumnName { get; init; } = "";
    public string DataTypeName { get; init; } = "";
    public Type DataType => DataTypeName switch
    {
        "bigint" => typeof(long),
        "bit" => typeof(bool),
        "decimal" or "money" or "numeric" or "smallmoney" => typeof(decimal),
        "int" => typeof(int),
        "smallint" => typeof(short),
        "tinyint" => typeof(byte),
        "float" => typeof(double),
        "real" => typeof(float),
        "datetime" or "datetime2" or "smalldatetime" => typeof(DateTime),
        "date" => typeof(DateOnly),
        "time" => typeof(TimeOnly),
        "char" or "varchar" or "text" or "nchar" or "nvarchar" or "ntext" => typeof(string),
        _ => typeof(object)
    };
    public bool IsNullable { get; init; } = true;
}