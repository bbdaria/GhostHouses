using System;

namespace WebServer.Models;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class FieldSpecAttribute : Attribute
{
    public string Category { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string? ForeignFieldId { get; set; }
    public bool IncludeInEventLog { get; set; }
}
