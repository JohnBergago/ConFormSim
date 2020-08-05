using UnityEngine;

public static class TryGetComponentExtension
{
    public static bool TryGetComponent<T>(this Transform trans, out T result) where T : Component
    {
        return (result = trans.GetComponent<T>()) != null;
    }

    public static bool TryGetComponent<T>(this GameObject obj, out T result) where T : Component
    {
        return (result = obj.GetComponent<T>()) != null;
    }

    
}