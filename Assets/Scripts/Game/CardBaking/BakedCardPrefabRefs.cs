using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class BakedCardPrefabRefs
{
    public const string CardExamplePath = "Assets/Prefabs/Cards/CardExample.prefab";
    public const string PlayerCardPath = "Assets/Prefabs/Cards/PlayerCard.prefab";
    public const string StandardCardPath = "Assets/Prefabs/Cards/Card.prefab";

    public static GameObject ResolveCardExample(GameObject assigned)
    {
        return Resolve(assigned, CardExamplePath);
    }

    public static GameObject ResolvePlayerCard(GameObject assigned)
    {
        return Resolve(assigned, PlayerCardPath);
    }

    public static GameObject ResolveStandardCard(GameObject assigned)
    {
        return Resolve(assigned, StandardCardPath);
    }

    public static GameObject Resolve(GameObject assigned, string assetPath)
    {
        if (assigned != null)
        {
            return assigned;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }
}
