using System;
using System.IO;
using System.Globalization;

public class YAMLDeserializer : IDeserializer, IDisposable
{
    public StreamReader file;
    public string BaseName { get; set; }

    public YAMLDeserializer(string filePath) { file = new(filePath); }
    public YAMLDeserializer(StreamReader file) { this.file = file; }
    public void Dispose() => file.Dispose();

    private string ReadYAMLLine(string name)
    {
        var fullName = BaseName != null ? $"{BaseName}.{name}" : name;
        var fieldName = fullName.Split('.')[^1];
        var lineStart = $"{new string('\t', fullName.PadLeft(1).PadRight(1).Split('.').Length - 1)}{fieldName}:";
        var line = file.ReadLine();
        if (!(line?.StartsWith(lineStart) ?? false))
        {
            Dispose();
            throw new Exception($"Could not read YAML line: {line}\n expected: {lineStart}");
        }
        return line.Substring(lineStart.Length);
    }
    public bool ReadBool(string name) => bool.Parse(ReadYAMLLine(name));
    public float ReadFloat(string name) => float.Parse(ReadYAMLLine(name), CultureInfo.InvariantCulture);
    public int ReadInt(string name) => int.Parse(ReadYAMLLine(name), CultureInfo.InvariantCulture);
    public string ReadString(string name) => ReadYAMLLine(name);


}