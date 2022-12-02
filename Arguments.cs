public class Arguments
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public int Port { get; set; }
    public string UserId { get; set; } = "";
    public string Password { get; set; } = "";
    public string[] Tables { get; set; } = new string[0];
    public bool IncludeDeletes { get; set; }
    public string? CsvDirectory { get; set; }
    public string SourceMode { get; set; } = "DB";
}