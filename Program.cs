using Microsoft.Data.SqlClient;

internal class Program
{
    private static void Main(string[] args)
    {
        Arguments arguments;
        try
        {
            arguments = Utilities.GetCommandLineArgs<Arguments>(args) ?? throw new ArgumentException("Unable to create arguments", nameof(args));
        }
        catch
        {
            Utilities.WriteUsage<Arguments>(Console.Out);
            throw;
        }

        SqlConnectionStringBuilder connectionStringBuilder = new();
        connectionStringBuilder.DataSource = arguments.Port != default ? $"{arguments.Server}:{arguments.Port}" : arguments.Server;
        connectionStringBuilder.InitialCatalog = arguments.Database;
        connectionStringBuilder.UserID = arguments.UserId;
        connectionStringBuilder.Password = arguments.Password;
        connectionStringBuilder.Encrypt = false; //https://github.com/dotnet/SqlClient/issues/1479
        connectionStringBuilder.TrustServerCertificate = true;

        using SqlConnection cn = new(connectionStringBuilder.ConnectionString);
        cn.Open();

        if (arguments.IncludeDeletes)
        {
            foreach (string table in arguments.Tables.Reverse())
            {
                Console.WriteLine($"delete {table}");
            }
        }

        using StatementBuilder builder = StatementBuilder.GetInstance(arguments.SourceMode, cn);

        if (builder is CsvSourcedStatementBuilder csvBuilder)
        {
            csvBuilder.CSVDirectoryPath = arguments.CsvDirectory; //This is a little hacky but simpler than the alternatives, I think
        }

        foreach (string table in arguments.Tables)
        {
            IEnumerable<ColumnInfo> columnInfos = builder.GetColumnInfo(table);
            foreach (string insertStatement in builder.GetInsertStatements(table, columnInfos))
            {
                Console.WriteLine(insertStatement);
            }
        }
    }
}