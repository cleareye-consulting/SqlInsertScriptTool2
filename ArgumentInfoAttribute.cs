
[AttributeUsage(AttributeTargets.Property)]
public class ArgumentInfoAttribute : Attribute
{
    public ArgumentInfoAttribute(string? alias = null, bool isRequired = false,
        bool promptIfMissing = false, bool isSecret = false,
        string? description = null, string[]? values = null)
    {
        Alias = alias;
        IsRequired = isRequired;
        PromptIfMissing = promptIfMissing;
        IsSecret = isSecret;
        Description = description;
        Values = values;
    }

    public string? Alias { get; }
    public bool IsRequired { get; }
    public bool PromptIfMissing { get; }
    public bool IsSecret { get; }
    public string? Description { get; }    
    public IEnumerable<string>? Values { get; }       

}