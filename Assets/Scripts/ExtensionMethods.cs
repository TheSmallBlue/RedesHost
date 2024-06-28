using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods 
{
    public static Vector3 CollapseAxis(this Vector3 source, int index)
    {
        source[index] = 0;
        return source;
    }

    public static List<SerializedDict<object, object>> SerializeDict(this Dictionary<object, object> dict)
    {
        List<SerializedDict<object, object>> returnList = new List<SerializedDict<object, object>>();

        foreach (var item in dict)
        {
            var serialDict = new SerializedDict<object,object>();

            serialDict.key = item.Key;
            serialDict.value = item.Value;

            returnList.Add(serialDict);
        }

        return default;
    }
}

public struct SerializedDict<T1, T2>
{
    public T1 key;
    public T2 value;
}
