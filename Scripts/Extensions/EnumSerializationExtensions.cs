public static class EnumSerializationExtensions
{
    public static void WriteEnum<T>(this ISerializer serializer, string name, T value) where T : System.Enum
        => serializer.WriteInt(name, System.Convert.ToInt32(value));

    public static T ReadEnum<T>(this IDeserializer deserializer, string name) where T : System.Enum
        => (T)System.Enum.ToObject(typeof(T), deserializer.ReadInt(name));
}