using System;

namespace CardDemo.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TraceAttribute : Attribute
    {
        public string Source { get; }
        public string? Rule { get; init; }
        public string? Ir { get; init; }

        public TraceAttribute(string source)
        {
            Source = source;
        }
    }
}
