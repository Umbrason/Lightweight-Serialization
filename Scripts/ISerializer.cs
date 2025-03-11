using System;

public interface ISerializer : IDisposable
{
    string BaseName { get; set; }
    void WriteInt(string name, int i);
    void WriteFloat(string name, float f);
    void WriteString(string name, string s);
    void WriteBool(string name, bool b);
    void WriteSerializable<T>(string name, T serializable) where T : ISerializable<T>
    {
        var oldBaseName = BaseName;        
        WriteString(name, serializable?.GetType()?.FullName ?? "null");
        BaseName = BaseName != null ? $"{BaseName}.{name}" : name;
        serializable?.Serialize(this);
        BaseName = oldBaseName;
    }
}