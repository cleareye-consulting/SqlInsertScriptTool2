using Microsoft.Data.SqlClient;

Arguments arguments = Utilities.GetCommandLineArgs<Arguments>(args) ?? throw new ArgumentException("Unable to create arguments", nameof(args));
string password = Utilities.GetPasswordFromConsole();

SqlConnectionStringBuilder connectionStringBuilder = new();
connectionStringBuilder.DataSource = arguments.Port != default ? $"{arguments.Server}:{arguments.Port}" : arguments.Server;
connectionStringBuilder.InitialCatalog = arguments.Database;
connectionStringBuilder.UserID = arguments.UserId;
connectionStringBuilder.Password = password;
connectionStringBuilder.Encrypt = false; //https://github.com/dotnet/SqlClient/issues/1479
connectionStringBuilder.TrustServerCertificate = true;

using SqlConnection cn = new(connectionStringBuilder.ConnectionString);
cn.Open();

foreach (string table in arguments.Tables.Reverse())
{
    Console.WriteLine($"delete {table}");
}

foreach (string table in arguments.Tables)
{
    IEnumerable<ColumnInfo> columnInfos = StatementBuilder.GetColumnInfo(table, cn);
    string select = StatementBuilder.GetSelectStatement(table, columnInfos, cn);
    foreach (string insertStatement in StatementBuilder.GetInsertStatements(table, select, columnInfos, cn))
    {
        Console.WriteLine(insertStatement);
    }
}
