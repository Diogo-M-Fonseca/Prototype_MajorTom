using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ShipColliderType { None, Box, Sphere }

public static class ShipPrefabUtility
{
    // Cria um prefab minimo (raiz + modelo) numa cena de preview, sem sujar as cenas abertas.
    public static void CreateBasePrefab(string prefabName, GameObject model, string path)
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject root = new GameObject(prefabName);
            SceneManager.MoveGameObjectToScene(root, previewScene);

            if (model != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, previewScene);
                visual.name = "Model";
                visual.transform.SetParent(root.transform, false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    public static void ConfigureRigidbody(Rigidbody body)
    {
        body.useGravity = false;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Ajusta um collider aos renderers. Nao faz nada se o prefab ja tiver um collider.
    public static void AddCollider(GameObject root, ShipColliderType type)
    {
        if (type == ShipColliderType.None)
            return;

        if (root.GetComponentInChildren<Collider>() != null)
        {
            Debug.Log($"Enemy Tools: '{root.name}' ja tem collider, nao adicionei outro.");
            return;
        }

        if (!TryGetLocalBounds(root, out Bounds bounds))
        {
            Debug.LogWarning($"Enemy Tools: '{root.name}' nao tem renderers, collider nao adicionado.");
            return;
        }

        if (type == ShipColliderType.Box)
        {
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }
        else
        {
            SphereCollider sphere = root.AddComponent<SphereCollider>();
            sphere.center = bounds.center;
            sphere.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }
    }

    // O ponto de disparo fica um pouco a frente do modelo, para o projetil nascer fora da nave.
    public static Transform GetOrCreateFiringPoint(GameObject root)
    {
        Transform existing = root.transform.Find("FiringPoint");
        if (existing != null)
            return existing;

        float distance = TryGetLocalBounds(root, out Bounds bounds) ? bounds.max.z + 0.5f : 1f;

        GameObject muzzle = new GameObject("FiringPoint");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, distance);

        return muzzle.transform;
    }

    public static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // Limites de todos os renderers, no espaco local da raiz.
    public static bool TryGetLocalBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;

        if (renderers.Length == 0)
            return false;

        Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;
        bool first = true;

        foreach (Renderer renderer in renderers)
        {
            Bounds world = renderer.bounds;
            Vector3 min = world.min;
            Vector3 max = world.max;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? min.x : max.x,
                    (i & 2) == 0 ? min.y : max.y,
                    (i & 4) == 0 ? min.z : max.z);

                Vector3 local = worldToLocal.MultiplyPoint3x4(corner);

                if (first)
                {
                    bounds = new Bounds(local, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(local);
                }
            }
        }

        return true;
    }
}
