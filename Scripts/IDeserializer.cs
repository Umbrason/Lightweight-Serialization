using System;
using System.Linq;

public interface IDeserializer : IDisposable
{
    string BaseName { get; set; }
    int ReadInt(string name);
    float ReadFloat(string name);
    string ReadString(string name);
    bool ReadBool(string name);
    T ReadSerializable<T>(string name) where T : ISerializable<T>
    {
        var oldBaseName = BaseName;
        var typeStr = ReadString(name);
        BaseName = BaseName != null ? $"{BaseName}.{name}" : name;
        var type = typeStr.ToLower() == "null" ? null : AppDomain.CurrentDomain.GetAssemblies().Select(Assembly => Assembly.GetType(typeStr)).FirstOrDefault(type => type != null);
        var isValidType = type != null && typeof(T).IsAssignableFrom(type);
        var value = isValidType ? ((T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type)).Deserialize(this) : default;
        BaseName = oldBaseName;
        return value;
    }
}