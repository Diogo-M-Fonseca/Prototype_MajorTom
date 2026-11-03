using System.IO;
using SpaceFlight;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Colocar numa pasta "Editor" (por exemplo Assets/Editor/RadarSetupWindow.cs).
// Menu: Tools > Radar > Radar Setup
public class RadarSetupWindow : EditorWindow
{
    private enum Corner { BottomRight, BottomLeft, TopRight, TopLeft }

    private const string AssetFolder = "Assets/Radar";
    private const string GeneratedFolder = "Assets/Radar/Generated";

    [SerializeField] private Corner _corner = Corner.BottomRight;
    [SerializeField] private float _size = 220f;
    [SerializeField] private float _margin = 30f;
    [SerializeField] private float _range = 150f;
    [SerializeField] private Transform _ship;

    [MenuItem("Tools/Radar/Radar Setup")]
    private static void Open()
    {
        RadarSetupWindow window = GetWindow<RadarSetupWindow>("Radar Setup");
        window.minSize = new Vector2(340f, 330f);
    }

    private void OnEnable()
    {
        if (_ship == null)
            AutoDetectShip();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        _corner = (Corner)EditorGUILayout.EnumPopup("Corner", _corner);
        _size = EditorGUILayout.Slider("Size (px)", _size, 100f, 400f);
        _margin = EditorGUILayout.Slider("Margin (px)", _margin, 0f, 100f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Radar", EditorStyles.boldLabel);
        _ship = (Transform)EditorGUILayout.ObjectField("Ship", _ship, typeof(Transform), true);
        _range = Mathf.Max(1f, EditorGUILayout.FloatField("Range", _range));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Cria o radar como um objeto novo. Nada do que já existe na cena é alterado. " +
            "Fica dentro do Canvas selecionado; sem seleção usa o primeiro Canvas da cena " +
            "(ou cria um novo se não houver nenhum).",
            MessageType.Info);

        if (GUILayout.Button("Create Radar UI", GUILayout.Height(30f)))
            CreateRadar();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Seleciona prefabs (Project) ou inimigos (Hierarchy) e adiciona o RadarTarget. " +
            "Só objetos com EnemyHealth são aceites. Instâncias na cena ficam com um override; o prefab não muda.",
            MessageType.None);

        if (GUILayout.Button("Add RadarTarget to Selected Enemies"))
            AddTargetsToSelection();
    }

    private void AutoDetectShip()
    {
        SpaceshipController controller = FindFirstObjectByType<SpaceshipController>();
        if (controller != null)
            _ship = controller.transform;
    }

    // ---------------------------------------------------------------- Create

    private void CreateRadar()
    {
        if (_ship == null)
            AutoDetectShip();

        EnsureFolder(AssetFolder);
        EnsureFolder(GeneratedFolder);

        Sprite disc = GetOrCreateSprite("RadarDisc", 256, DiscAlpha);
        Sprite grid = GetOrCreateSprite("RadarGrid", 256, GridAlpha);
        Sprite dot = GetOrCreateSprite("RadarDot", 64, DotAlpha);
        Sprite arrowUp = GetOrCreateSprite("RadarArrowUp", 64, ArrowUpAlpha);
        Sprite arrowDown = GetOrCreateSprite("RadarArrowDown", 64, (u, v, px) => ArrowUpAlpha(u, -v, px));
        Image blipPrefab = GetOrCreateBlipPrefab(dot);

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Radar UI");

        Transform parent = ResolveParent();

        RectTransform root = CreateRect(GameObjectUtility.GetUniqueNameForSibling(parent, "Radar"), parent);
        ApplyCorner(root);

        RectTransform background = CreateRect("Background", root);
        StretchFull(background);
        AddImage(background, disc, new Color(0.02f, 0.08f, 0.1f, 0.6f));

        RectTransform gridRect = CreateRect("Grid", root);
        StretchFull(gridRect);
        AddImage(gridRect, grid, new Color(0.3f, 1f, 0.8f, 0.8f));

        RectTransform shipMarker = CreateRect("Ship Marker", root);
        shipMarker.anchorMin = shipMarker.anchorMax = shipMarker.pivot = new Vector2(0.5f, 0.5f);
        shipMarker.anchoredPosition = Vector2.zero;
        shipMarker.sizeDelta = new Vector2(14f, 14f);
        AddImage(shipMarker, arrowUp, Color.white);

        RectTransform blips = CreateRect("Blips", root);
        StretchFull(blips);
        blips.offsetMin = new Vector2(8f, 8f);
        blips.offsetMax = new Vector2(-8f, -8f);

        RadarUI radar = Undo.AddComponent<RadarUI>(root.gameObject);
        ConfigureRadar(radar, blips, blipPrefab, arrowUp, dot, arrowDown);

        Selection.activeGameObject = root.gameObject;
        EditorGUIUtility.PingObject(root.gameObject);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Undo.CollapseUndoOperations(group);

        if (_ship == null)
            Debug.LogWarning("Radar Setup: não encontrei nenhuma nave. Atribui o campo 'Ship' no RadarUI.", root);
    }

    private void ConfigureRadar(RadarUI radar, RectTransform container, Image blipPrefab,
        Sprite above, Sprite level, Sprite below)
    {
        SerializedObject so = new SerializedObject(radar);

        SetObject(so, "_ship", _ship);
        SetBool(so, "_invertForward", ReadInvertForward(_ship));
        SetObject(so, "_container", container);
        SetObject(so, "_blipPrefab", blipPrefab);
        SetObject(so, "_aboveSprite", above);
        SetObject(so, "_levelSprite", level);
        SetObject(so, "_belowSprite", below);

        SerializedProperty range = so.FindProperty("_range");
        if (range != null)
            range.floatValue = _range;

        so.ApplyModifiedProperties();
    }

    private static void SetObject(SerializedObject so, string field, Object value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
        {
            Debug.LogWarning($"Radar Setup: o campo '{field}' não existe no RadarUI.");
            return;
        }

        property.objectReferenceValue = value;
    }

    private static void SetBool(SerializedObject so, string field, bool value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property != null)
            property.boolValue = value;
    }

    private static bool ReadInvertForward(Transform ship)
    {
        if (ship != null)
        {
            SpaceshipController controller = ship.GetComponent<SpaceshipController>();
            if (controller != null)
            {
                SerializedProperty property = new SerializedObject(controller).FindProperty("invertForward");
                if (property != null)
                    return property.boolValue;
            }
        }

        return true;
    }

    private void ApplyCorner(RectTransform rect)
    {
        Vector2 anchor;
        Vector2 position;

        switch (_corner)
        {
            case Corner.BottomLeft:
                anchor = new Vector2(0f, 0f);
                position = new Vector2(_margin, _margin);
                break;
            case Corner.TopRight:
                anchor = new Vector2(1f, 1f);
                position = new Vector2(-_margin, -_margin);
                break;
            case Corner.TopLeft:
                anchor = new Vector2(0f, 1f);
                position = new Vector2(_margin, -_margin);
                break;
            default:
                anchor = new Vector2(1f, 0f);
                position = new Vector2(-_margin, _margin);
                break;
        }

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(_size, _size);
    }

    // ------------------------------------------------------------ Hierarchy

    private static Transform ResolveParent()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null
            && !EditorUtility.IsPersistent(selected)
            && selected.transform is RectTransform
            && selected.GetComponentInParent<Canvas>() != null)
        {
            return selected.transform;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
                return canvas.transform;
        }

        GameObject canvasObject = new GameObject("Radar Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Radar Canvas");
        canvasObject.layer = LayerMask.NameToLayer("UI");

        Canvas newCanvas = canvasObject.GetComponent<Canvas>();
        newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvasObject.transform;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + objectName);
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return (RectTransform)go.transform;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddImage(RectTransform rect, Sprite sprite, Color color)
    {
        Image image = Undo.AddComponent<Image>(rect.gameObject);
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
    }

    // --------------------------------------------------------------- Assets

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Image GetOrCreateBlipPrefab(Sprite dot)
    {
        string path = $"{GeneratedFolder}/RadarBlip.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing.GetComponent<Image>();

        GameObject go = new GameObject("RadarBlip", typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(16f, 16f);

        Image image = go.GetComponent<Image>();
        image.sprite = dot;
        image.raycastTarget = false;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);

        return saved != null ? saved.GetComponent<Image>() : null;
    }

    private delegate float AlphaFunc(float u, float v, float px);

    // Nunca sobrescreve: se o PNG já existir, é reutilizado (podes substituí-lo por arte tua).
    private static Sprite GetOrCreateSprite(string spriteName, int size, AlphaFunc alphaAt)
    {
        string path = $"{GeneratedFolder}/{spriteName}.png";

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        string fullPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), path);
        if (!File.Exists(fullPath))
            WriteTexture(fullPath, size, alphaAt);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void WriteTexture(string fullPath, int size, AlphaFunc alphaAt)
    {
        float px = 2f / size;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (y + 0.5f) / size * 2f - 1f;

            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaAt(u, v, px)));
            }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.SetPixels(pixels);
        texture.Apply();

        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        DestroyImmediate(texture);
    }

    // ------------------------------------------------- Formas (coords -1..1)

    // Cobertura anti-aliased a partir da distância (positiva = dentro da forma).
    private static float Coverage(float distanceInside, float px)
    {
        return Mathf.Clamp01(distanceInside / px + 0.5f);
    }

    private static float DiscAlpha(float u, float v, float px)
    {
        return Coverage(0.98f - Mathf.Sqrt(u * u + v * v), px);
    }

    private static float DotAlpha(float u, float v, float px)
    {
        return Coverage(0.85f - Mathf.Sqrt(u * u + v * v), px);
    }

    private static float GridAlpha(float u, float v, float px)
    {
        float r = Mathf.Sqrt(u * u + v * v);

        float outer = Coverage(0.015f - Mathf.Abs(r - 0.965f), px);
        float inner = Coverage(0.007f - Mathf.Abs(r - 0.5f), px) * 0.6f;
        float inside = Coverage(0.95f - r, px);
        float crossH = Coverage(0.007f - Mathf.Abs(v), px) * 0.5f * inside;
        float crossV = Coverage(0.007f - Mathf.Abs(u), px) * 0.5f * inside;

        return Mathf.Max(Mathf.Max(outer, inner), Mathf.Max(crossH, crossV));
    }

    private static float ArrowUpAlpha(float u, float v, float px)
    {
        Vector2 p = new Vector2(u, v);
        Vector2 a = new Vector2(0f, 0.9f);
        Vector2 b = new Vector2(-0.75f, -0.8f);
        Vector2 c = new Vector2(0.75f, -0.8f);

        float d = Mathf.Min(EdgeDistance(p, a, b), Mathf.Min(EdgeDistance(p, b, c), EdgeDistance(p, c, a)));
        return Coverage(d, px);
    }

    // Triângulo no sentido anti-horário: a normal interior aponta para a esquerda da aresta.
    private static float EdgeDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 edge = b - a;
        Vector2 normal = new Vector2(-edge.y, edge.x).normalized;
        return Vector2.Dot(p - a, normal);
    }

    // -------------------------------------------------------------- Enemies

    private static void AddTargetsToSelection()
    {
        int added = 0;
        int skipped = 0;

        foreach (GameObject go in Selection.gameObjects)
        {
            if (EditorUtility.IsPersistent(go))
            {
                string path = AssetDatabase.GetAssetPath(go);

                using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    GameObject prefabRoot = scope.prefabContentsRoot;

                    if (prefabRoot.GetComponentInChildren<EnemyHealth>(true) == null
                        || prefabRoot.GetComponent<RadarTarget>() != null)
                    {
                        skipped++;
                        continue;
                    }

                    prefabRoot.AddComponent<RadarTarget>();
                    added++;
                }

                continue;
            }

            if (go.GetComponentInChildren<EnemyHealth>(true) == null || go.GetComponent<RadarTarget>() != null)
            {
                skipped++;
                continue;
            }

            Undo.AddComponent<RadarTarget>(go);
            added++;
        }

        Debug.Log($"Radar Setup: RadarTarget adicionado a {added} objeto(s); {skipped} ignorado(s) " +
                  "(sem EnemyHealth ou já tinham RadarTarget).");
    }
}
