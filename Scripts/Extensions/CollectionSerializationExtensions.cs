using System;
using System.Collections.Generic;
using System.Linq;

public static class CollectionSerializationExtensions
{
    public static void WriteDict<K, V>(this ISerializer serializer, string name, IReadOnlyDictionary<K, V> dict, Action<string, K> writeKey, Action<string, V> writeValue)
    {
        serializer.WriteString(name, dict?.GetType()?.Name ?? "null");
        if (dict == null) return;
        serializer.WriteInt($"{name}.Size", dict.Count);
        int i = 0;
        foreach (var pair in dict)
        {
            writeKey($"{name}.Key[{i}]", pair.Key);
            writeValue($"{name}.Value[{i}]", pair.Value);
            i++;
        }
    }
    public static void WriteList<T>(this ISerializer serializer, string name, IReadOnlyList<T> list, Action<string, T> writeElement)
    {
        serializer.WriteString(name, list?.GetType()?.Name ?? "null");
        if (list == null) return;
        serializer.WriteInt($"{name}.Size", list.Count);
        for (int i = 0; i < list.Count; i++)
            writeElement($"{name}.Item[{i}]", list[i]);
    }
    public static void WriteArray<T>(this ISerializer serializer, string name, T[] array, Action<string, T> writeElement)
    {
        serializer.WriteString(name, array?.GetType()?.Name ?? "null");
        if (array == null) return;
        serializer.WriteInt($"{name}.Size", array.Length);
        for (int i = 0; i < array.Length; i++)
            writeElement($"{name}.Item[{i}]", array[i]);
    }

    public static Dictionary<K, V> ReadDict<K, V>(this IDeserializer deserializer, string name, Func<string, K> readKey, Func<string, V> readValue)
    {
        var type = deserializer.ReadString(name);
        if (type == "null") return null;
        var dict = new Dictionary<K, V>();
        var dictCount = deserializer.ReadInt($"{name}.Size");
        for (int i = 0; i < dictCount; i++)
        {
            var pairKey = readKey($"{name}.Key[{i}]");
            var pairValue = readValue($"{name}.Value[{i}]");
            dict.Add(pairKey, pairValue);
        }
        return dict;
    }
    public static List<T> ReadList<T>(this IDeserializer deserializer, string name, Func<string, T> readElement)
    {
        var type = deserializer.ReadString(name);
        if (type == "null") return null;
        var listCount = deserializer.ReadInt($"{name}.Size");
        var list = new List<T>(listCount);
        for (int i = 0; i < listCount; i++)
            list.Add(readElement($"{name}.Item[{i}]"));
        return list;
    }
    public static T[] ReadArray<T>(this IDeserializer deserializer, string name, Func<string, T> readElement)
    {
        var type = deserializer.ReadString(name);
        if (type == "null") return null;
        var arrayLength = deserializer.ReadInt($"{name}.Size");
        var array = new T[arrayLength];
        for (int i = 0; i < arrayLength; i++)
            array[i] = readElement($"{name}.Item[{i}]");
        return array;
    }

    //TODO: dont assume that all keys exist, rather read COUNT lines and parse the keys from the lines
    public static Array ReadNDArray<T>(this IDeserializer deserializer, string name, Func<string, T> readElement)
    {
        var type = deserializer.ReadString(name);
        if (type == "null") return null;
        string dimensions = deserializer.ReadString($"{name}.Size");
        var dimParams = dimensions.Split(',').Select(int.Parse).ToArray();
        var arr = Array.CreateInstance(typeof(T), dimParams);
        var arrayLength = dimParams.Aggregate(1, (a, b) => a * b);
        for (int i = 0; i < arrayLength; i++)
        {
            int[] index = NDArrayIndexFromMemPos(i, dimParams);
            arr.SetValue(readElement($"{name}.Item[{string.Join(", ", index)}]"), index);
        }
        return arr;
    }

    //TODO: dont write null values
    public static void WriteNDArray<T>(this ISerializer serializer, string name, Array array, Action<string, T> writeElement)
    {
        serializer.WriteString(name, array?.GetType()?.Name ?? "null");
        if (array == null) return;
        var dimParams = Enumerable.Range(0, array.Rank).Select(array.GetLength).ToArray();
        serializer.WriteString($"{name}.Size", string.Join(", ", dimParams));
        var arrayLength = dimParams.Aggregate(1, (a, b) => a * b);
        for (int i = 0; i < arrayLength; i++)
        {
            int[] index = NDArrayIndexFromMemPos(i, dimParams);
            writeElement($"{name}.Item[{string.Join(", ", index)}]", (T)array.GetValue(index));
        }
    }

    private static int[] NDArrayIndexFromMemPos(int memPos, int[] dimensions)
    {
        int[] index = new int[dimensions.Length];
        for (int i = 0; i < dimensions.Length; i++)
        {
            var elementsBefore = 1;
            for (int j = 0; j < i; j++)
                elementsBefore *= dimensions[j];
            index[i] = memPos / elementsBefore;
            index[i] %= dimensions[i];
        }
        return index;
    }
}