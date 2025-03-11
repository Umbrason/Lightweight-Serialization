public interface ISerializable<T> where T : ISerializable<T>
{
    void Serialize(ISerializer serializer) => ReflectionSerializer.Serialize(this, serializer);
    T Deserialize(IDeserializer deserializer) => ReflectionSerializer.Deserialize((T)this, deserializer);
}
