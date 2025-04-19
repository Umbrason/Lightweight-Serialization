using System;
using System.IO;
using System.Globalization;

public class YAMLSerializer : ISerializer, IDisposable
{
    public StreamWriter file;
    public string BaseName { get; set; }

    public YAMLSerializer(string filePath) { file = new(filePath); }
    public YAMLSerializer(StreamWriter file) { this.file = file; }
    public void Dispose() => file.Dispose();

    private void WriteYAMLLine(string name, string value)
    {
        var fullName = BaseName != null ? $"{BaseName}.{name}" : name;
        var fieldName = fullName.Split('.')[^1];
        var lineStart = $"{new string('\t', fullName.PadLeft(1).PadRight(1).Split('.').Length - 1)}{fieldName}:";
        file.WriteLine($"{lineStart}{value}");
    }
    public void WriteBool(string name, bool b) => WriteYAMLLine(name, b.ToString(CultureInfo.InvariantCulture));
    public void WriteFloat(string name, float f) => WriteYAMLLine(name, f.ToString(CultureInfo.InvariantCulture));
    public void WriteInt(string name, int i) => WriteYAMLLine(name, i.ToString(CultureInfo.InvariantCulture));
    public void WriteString(string name, string s) => WriteYAMLLine(name, s);
}