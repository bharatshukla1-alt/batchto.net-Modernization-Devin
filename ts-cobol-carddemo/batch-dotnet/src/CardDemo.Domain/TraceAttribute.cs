namespace CardDemo.Domain;

/// <summary>Links target code to the legacy source element (program, paragraph, copybook, JCL step) or rule id it implements.</summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
public sealed class TraceAttribute(string source, string? rule = null) : Attribute
{
    public string Source { get; } = source;
    public string? Rule { get; } = rule;
}
