namespace DapperContext.Attributes;

//Example of giving an attribute the Ignore statement
//[BulkIgnore]
//public int Id { get; set; }

[AttributeUsage(AttributeTargets.Property)]
public class DataRowIgnoreAttribute : Attribute
{
    private readonly bool _ignore;

    public bool Ignore { get => _ignore; }

    public DataRowIgnoreAttribute(bool ignore = true)
    {
        _ignore = ignore;
    }
}