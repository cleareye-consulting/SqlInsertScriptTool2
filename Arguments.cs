public class Arguments
{

    [ArgumentInfo(alias: "s", isRequired: true, description: "The server to connect to for metadata, and possibly the data")]
    public string Server { get; set; } = "";

    [ArgumentInfo(alias: "d", isRequired: true, description: "The database that contains the metadata, and possibly the data")]
    public string Database { get; set; } = "";

    public int? Port { get; set; }

    [ArgumentInfo(alias: "u", isRequired: true)]
    public string UserId { get; set; } = "";

    [ArgumentInfo(alias: "p", isRequired: true, promptIfMissing:true, isSecret: true)]
    public string Password { get; set; } = "";

    [ArgumentInfo(alias: "t", isRequired: true, description: "Comma-separated list of tables to generate statements for")]
    public string[] Tables { get; set; } = new string[0];

    [ArgumentInfo(alias: "incdel", isRequired: false)]
    public bool IncludeDeletes { get; set; }

    [ArgumentInfo(alias: "csvdir", isRequired: false)]
    public string? CsvDirectory { get; set; }

    [ArgumentInfo(alias: "src", isRequired: false, values: new[] { "DB", "CSV" })]
    public string SourceMode { get; set; } = "DB";
}