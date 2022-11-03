public class Arguments
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public int Port { get; set; }
    public string UserId { get; set; } = "";
    public string[] Tables { get; set; } = new string[0];
}