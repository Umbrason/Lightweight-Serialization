using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public static class ReflectionSerializer
{

    static readonly Regex backingFieldNameSanitizer = new(@".*<(.*)>.*");
    public static void Serialize<T>(ISerializable<T> serializable, ISerializer serializer) where T : ISerializable<T>
    {
        var type = serializable.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var serializableMembers = fields.OrderBy(m => m.MetadataToken).ToArray();

        for (int i = 0; i < serializableMembers.Length; i++)
        {
            var field = serializableMembers[i];
            var fieldType = field.FieldType;
            var fieldValue = field.GetValue(serializable);
            var fieldName = field.Name;
            var match = backingFieldNameSanitizer.Match(fieldName);
            if (match.Success) fieldName = match.Groups[1].Value;
            SerializeField(fieldType, fieldName, fieldValue, serializer);
        }
    }

    public static T Deserialize<T>(T deserializable, IDeserializer deserializer) where T : ISerializable<T>
    {
        var obj = (object)deserializable; //boxing for struct support
        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var serializableMembers = fields.OrderBy(m => m.MetadataToken).ToArray();

        for (int i = 0; i < serializableMembers.Length; i++)
        {
            var field = serializableMembers[i];
            var fieldType = field.FieldType;
            var fieldName = field.Name;
            var match = backingFieldNameSanitizer.Match(fieldName);
            if (match.Success) fieldName = match.Groups[1].Value;
            var fieldValue = DeserializeField(fieldType, fieldName, deserializer);
            field.SetValue(obj, fieldValue);
        }
        return (T)obj;
    }


    public static void SerializeFieldGeneric<T>(ISerializer serializer, string name, T value) => SerializeField(typeof(T), name, value, serializer);
    public static void SerializeField(Type type, string name, object value, ISerializer serializer)
    {
        #region Primitives
        if (type == typeof(int)) serializer.WriteInt(name, (int)value);
        else if (type == typeof(uint)) serializer.WriteInt(name, Convert.ToInt32(value));
        else if (type == typeof(float)) serializer.WriteFloat(name, (float)value);
        else if (type == typeof(string)) serializer.WriteString(name, (string)value);
        else if (type == typeof(bool)) serializer.WriteBool(name, (bool)value);
        else if (type.IsEnum) serializer.WriteString(name, value.ToString());
        #endregion

        #region Collections
        else if (type.IsArray)
        {
            var collectionSerializationExtensions = typeof(CollectionSerializationExtensions);
            var rank = type.GetArrayRank();
            var elementType = type.GetElementType();
            var writeElement = typeof(ReflectionSerializer).GetMethod(nameof(SerializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);
            var callbackType = typeof(Action<,>).MakeGenericType(typeof(string), elementType);
            var writeElementCallback = Delegate.CreateDelegate(callbackType, serializer, writeElement);
            if (rank == 1)
                collectionSerializationExtensions
                .GetMethod(nameof(CollectionSerializationExtensions.WriteArray))
                .MakeGenericMethod(elementType)
                .Invoke(serializer, new object[] { serializer, name, value, writeElementCallback }); //1-D
            else collectionSerializationExtensions
                .GetMethod(nameof(CollectionSerializationExtensions.WriteNDArray))
                .MakeGenericMethod(elementType)
                .Invoke(serializer, new[] { serializer, name, value, writeElementCallback }); //N-D
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var collectionSerializationExtensions = typeof(CollectionSerializationExtensions);
            var elementType = type.GetGenericArguments()[0];
            var writeElement = typeof(ReflectionSerializer).GetMethod(nameof(SerializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);
            var callbackType = typeof(Action<,>).MakeGenericType(typeof(string), elementType);
            var writeElementCallback = Delegate.CreateDelegate(callbackType, serializer, writeElement);
            collectionSerializationExtensions
            .GetMethod(nameof(CollectionSerializationExtensions.WriteList))
            .MakeGenericMethod(elementType)
            .Invoke(serializer, new object[] { serializer, name, value, writeElementCallback });
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var collectionSerializationExtensions = typeof(CollectionSerializationExtensions);
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            var writeKeyMethodInfo = typeof(ReflectionSerializer).GetMethod(nameof(SerializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(keyType);
            var writeKeyCallbackType = typeof(Action<,>).MakeGenericType(typeof(string), keyType);
            var writeKeyCallback = Delegate.CreateDelegate(writeKeyCallbackType, serializer, writeKeyMethodInfo);

            var writeValueMethodInfo = typeof(ReflectionSerializer).GetMethod(nameof(SerializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(valueType);
            var writeValueCallbackType = typeof(Action<,>).MakeGenericType(typeof(string), valueType);
            var writeValueCallback = Delegate.CreateDelegate(writeValueCallbackType, serializer, writeValueMethodInfo);

            collectionSerializationExtensions
            .GetMethod(nameof(CollectionSerializationExtensions.WriteDict))
            .MakeGenericMethod(keyType, valueType)
            .Invoke(serializer, new object[] { serializer, name, value, writeKeyCallback, writeValueCallback });
        }
        #endregion
        else if (typeof(ISerializable<>).MakeGenericType(type).IsAssignableFrom(type))
        {
            var writeMethodInfo = typeof(ISerializer).GetMethod(nameof(ISerializer.WriteSerializable)).MakeGenericMethod(type);
            writeMethodInfo.Invoke(serializer, new[] { name, value });
        }
        else UnityEngine.Debug.LogError($"Unsupported type for serialization: {type}");
    }

    public static T DeserializeFieldGeneric<T>(IDeserializer deserializer, string name) => (T)DeserializeField(typeof(T), name, deserializer);
    public static object DeserializeField(Type type, string name, IDeserializer deserializer)
    {
        #region Primitives
        if (type == typeof(int)) return deserializer.ReadInt(name);
        if (type == typeof(uint)) return (uint)deserializer.ReadInt(name);
        if (type == typeof(float)) return deserializer.ReadFloat(name);
        if (type == typeof(string)) return deserializer.ReadString(name);
        if (type == typeof(bool)) return deserializer.ReadBool(name);
        if (type.IsEnum) return Enum.Parse(type, deserializer.ReadString(name));
        #endregion

        #region Collections
        var collectionDeserializationExtensions = typeof(CollectionSerializationExtensions);

        if (type.IsArray)
        {
            var rank = type.GetArrayRank();
            var elementType = type.GetElementType();
            var readElement = typeof(ReflectionSerializer).GetMethod(nameof(DeserializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);
            var callbackType = typeof(Func<,>).MakeGenericType(typeof(string), elementType);
            var readElementCallback = Delegate.CreateDelegate(callbackType, deserializer, readElement);

            if (rank == 1)
            {
                return collectionDeserializationExtensions
                    .GetMethod(nameof(CollectionSerializationExtensions.ReadArray))
                    .MakeGenericMethod(elementType)
                    .Invoke(deserializer, new object[] { deserializer, name, readElementCallback }); //1-D
            }
            else
            {
                return collectionDeserializationExtensions
                    .GetMethod(nameof(CollectionSerializationExtensions.ReadNDArray))
                    .MakeGenericMethod(elementType)
                    .Invoke(deserializer, new object[] { deserializer, name, readElementCallback }); //N-D
            }
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = type.GetGenericArguments()[0];
            var readElement = typeof(ReflectionSerializer).GetMethod(nameof(DeserializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(elementType);
            var callbackType = typeof(Func<,>).MakeGenericType(typeof(string), elementType);
            var readElementCallback = Delegate.CreateDelegate(callbackType, deserializer, readElement);

            return collectionDeserializationExtensions
                .GetMethod(nameof(CollectionSerializationExtensions.ReadList))
                .MakeGenericMethod(elementType)
                .Invoke(deserializer, new object[] { deserializer, name, readElementCallback });
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            var readKeyMethodInfo = typeof(ReflectionSerializer).GetMethod(nameof(DeserializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(keyType);
            var readKeyCallbackType = typeof(Func<,>).MakeGenericType(typeof(string), keyType);
            var readKeyCallback = Delegate.CreateDelegate(readKeyCallbackType, deserializer, readKeyMethodInfo);

            var readValueMethodInfo = typeof(ReflectionSerializer).GetMethod(nameof(DeserializeFieldGeneric), BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(valueType);
            var readValueCallbackType = typeof(Func<,>).MakeGenericType(typeof(string), valueType);
            var readValueCallback = Delegate.CreateDelegate(readValueCallbackType, deserializer, readValueMethodInfo);

            return collectionDeserializationExtensions
                .GetMethod(nameof(CollectionSerializationExtensions.ReadDict))
                .MakeGenericMethod(keyType, valueType)
                .Invoke(deserializer, new object[] { deserializer, name, readKeyCallback, readValueCallback });
        }
        #endregion

        // Check for serializable types
        else if (typeof(ISerializable<>).MakeGenericType(type).IsAssignableFrom(type))
        {
            var readMethodInfo = typeof(IDeserializer).GetMethod(nameof(IDeserializer.ReadSerializable)).MakeGenericMethod(type);
            var deserialized = readMethodInfo.Invoke(deserializer, new object[] { name });
            return deserialized;
        }

        UnityEngine.Debug.LogError($"Unsupported type for deserialization: {type}");
        return default;
    }
}
