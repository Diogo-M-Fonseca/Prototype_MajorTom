using UnityEditor;
using UnityEngine;

public static class EditorAssetUtility
{
    public static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    // Reaproveita o asset se ja existir com este nome na pasta; senao cria um novo.
    public static T LoadOrCreateAsset<T>(string folder, string assetName) where T : ScriptableObject
    {
        EnsureFolder(folder);
        string path = $"{folder}/{assetName}.asset";

        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // Escrevem em campos [SerializeField] privados sem os tornar public.
    public static void SetObject(Object target, string field, Object value) =>
        Apply(target, field, property => property.objectReferenceValue = value);

    public static void SetFloat(Object target, string field, float value) =>
        Apply(target, field, property => property.floatValue = value);

    public static void SetInt(Object target, string field, int value) =>
        Apply(target, field, property => property.intValue = value);

    public static void SetString(Object target, string field, string value) =>
        Apply(target, field, property => property.stringValue = value);

    private static void Apply(Object target, string field, System.Action<SerializedProperty> setter)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
        {
            Debug.LogWarning($"Enemy Tools: campo '{field}' nao encontrado em {target.GetType().Name}.");
            return;
        }

        setter(property);
        serialized.ApplyModifiedProperties();
    }

    // Devolve o indice da layer, criando-a no primeiro espaco livre se ainda nao existir.
    public static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing != -1)
            return existing;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0)
        {
            Debug.LogError("Enemy Tools: nao consegui abrir o TagManager.");
            return -1;
        }

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // As layers 0-7 sao reservadas pelo Unity.
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);

            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogError($"Enemy Tools: nao ha espacos livres para criar a layer '{layerName}'.");
        return -1;
    }
}
