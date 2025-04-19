public static class EnumSerializationExtensions
{
    public static void WriteEnum<T>(this ISerializer serializer, string name, T value) where T : Enum
        => serializer.WriteInt(name, (int)value);

    public static T ReadEnum<T>(this IDeserializer deserializer, string name) where T : Enum
        => (T)deserializer.ReadInt(name);
}