using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mostra um campo de texto (caminho de pasta) como um menu com as pastas que já existem em Assets.
public class FolderPopupAttribute : PropertyAttribute
{
}

// Desenha os campos marcados com [FolderPopup] como um botão que abre o menu das pastas de Assets.
// As pastas com subpastas viram submenus; a própria pasta aparece como "Esta pasta".
[CustomPropertyDrawer(typeof(FolderPopupAttribute))]
public class FolderPopupDrawer : PropertyDrawer
{
    private const string RootFolder = "Assets";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Usa [FolderPopup] num campo de texto.");
            return;
        }

        Rect fieldRect = EditorGUI.PrefixLabel(position, label);
        string current = property.stringValue;
        string shown = string.IsNullOrEmpty(current) ? "Escolher pasta..." : current;

        if (EditorGUI.DropdownButton(fieldRect, new GUIContent(shown), FocusType.Keyboard))
            ShowMenu(fieldRect, property.serializedObject, property.propertyPath, current);
    }

    private static void ShowMenu(Rect rect, SerializedObject serialized, string propertyPath, string current)
    {
        List<string> folders = new List<string>();
        CollectFolders(RootFolder, folders);

        GenericMenu menu = new GenericMenu();

        foreach (string folder in folders)
        {
            bool hasChildren = AssetDatabase.GetSubFolders(folder).Length > 0;
            string menuPath = hasChildren ? $"{folder}/Esta pasta" : folder;
            string chosen = folder;

            menu.AddItem(new GUIContent(menuPath), folder == current, () => Apply(serialized, propertyPath, chosen));
        }

        menu.DropDown(rect);
    }

    private static void CollectFolders(string folder, List<string> result)
    {
        result.Add(folder);

        foreach (string sub in AssetDatabase.GetSubFolders(folder))
            CollectFolders(sub, result);
    }

    private static void Apply(SerializedObject serialized, string propertyPath, string folder)
    {
        if (serialized == null || serialized.targetObject == null)
            return;

        serialized.Update();
        serialized.FindProperty(propertyPath).stringValue = folder;
        serialized.ApplyModifiedProperties();
    }
}

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

    public static void SetBool(Object target, string field, bool value) =>
        Apply(target, field, property => property.boolValue = value);

    public static void SetEnum(Object target, string field, int index) =>
        Apply(target, field, property => property.enumValueIndex = index);

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

public readonly struct EnemyDataSet
{
    public ShipStats Stats { get; }
    public MovementProfile Movement { get; }
    public BehaviourProfile FlockProfile { get; }
    public BehaviourProfile SoloProfile { get; }
    public GunData Gun { get; }
    public AIState InitialState { get; }

    public EnemyDataSet(ShipStats stats, MovementProfile movement,
        BehaviourProfile flockProfile, BehaviourProfile soloProfile, GunData gun, AIState initialState)
    {
        Stats = stats;
        Movement = movement;
        FlockProfile = flockProfile;
        SoloProfile = soloProfile;
        Gun = gun;
        InitialState = initialState;
    }
}

public static class EnemyDataFactory
{
    // Estrutura de pastas:
    //   <pasta>/Shared/           assets sem parametros (Separation, Alignment, Cohesion, Flee, StateTimeUp), partilhados
    //   <pasta>/<nome>/           tudo o que e especifico deste tipo de inimigo
    //   <pasta>/<nome>/FSM/       estados, perfis por estado e condicoes das transicoes
    // Se um asset com o mesmo nome ja existir, e reaproveitado e os valores sao atualizados.
    public static EnemyDataSet Create(EnemyDataSettings settings)
    {
        string root = settings.Folder.TrimEnd('/');
        string sharedFolder = $"{root}/Shared";
        string typeFolder = $"{root}/{settings.Name}";
        string name = settings.Name;

        // Stats
        ShipStats stats = EditorAssetUtility.LoadOrCreateAsset<ShipStats>(typeFolder, $"{name}_Stats");
        EditorAssetUtility.SetFloat(stats, "_maxHealth", settings.MaxHealth);

        // Movement
        MovementProfile movement = EditorAssetUtility.LoadOrCreateAsset<MovementProfile>(typeFolder, $"{name}_Movement");
        EditorAssetUtility.SetFloat(movement, "_minSpeed", settings.MinSpeed);
        EditorAssetUtility.SetFloat(movement, "_maxSpeed", settings.MaxSpeed);
        EditorAssetUtility.SetFloat(movement, "_acceleration", settings.Acceleration);
        EditorAssetUtility.SetFloat(movement, "_lateralGrip", settings.LateralGrip);
        EditorAssetUtility.SetFloat(movement, "_maxTurnRate", settings.MaxTurnRate);

        // Behaviours partilhados (sem parametros)
        SeparationBehaviour separation = EditorAssetUtility.LoadOrCreateAsset<SeparationBehaviour>(sharedFolder, "Separation");
        AlignmentBehaviour alignment = EditorAssetUtility.LoadOrCreateAsset<AlignmentBehaviour>(sharedFolder, "Alignment");
        CohesionBehaviour cohesion = EditorAssetUtility.LoadOrCreateAsset<CohesionBehaviour>(sharedFolder, "Cohesion");

        // Behaviours com parametros: um por tipo de inimigo
        PursueTargetBehaviour pursue = EditorAssetUtility.LoadOrCreateAsset<PursueTargetBehaviour>(typeFolder, $"{name}_Pursue");
        EditorAssetUtility.SetFloat(pursue, "_leadFactor", settings.LeadFactor);

        AvoidObstaclesBehaviour avoid = EditorAssetUtility.LoadOrCreateAsset<AvoidObstaclesBehaviour>(typeFolder, $"{name}_Avoid");
        EditorAssetUtility.SetInt(avoid, "_obstacleMask", settings.ObstacleMask.value);
        EditorAssetUtility.SetFloat(avoid, "_distance", settings.AvoidDistance);
        EditorAssetUtility.SetFloat(avoid, "_radius", settings.AvoidRadius);

        // Perfis estaticos (usados quando o prefab nao tem FSM)
        BehaviourProfile flockProfile = null;
        BehaviourProfile soloProfile = null;

        if (settings.CreateFlockProfile)
        {
            flockProfile = EditorAssetUtility.LoadOrCreateAsset<BehaviourProfile>(typeFolder, $"{name}_Flock");
            ConfigureProfile(flockProfile, settings,
                (pursue, settings.PursueWeight),
                (separation, settings.SeparationWeight),
                (alignment, settings.AlignmentWeight),
                (cohesion, settings.CohesionWeight),
                (avoid, settings.AvoidWeight));
        }

        if (settings.CreateSoloProfile)
        {
            soloProfile = EditorAssetUtility.LoadOrCreateAsset<BehaviourProfile>(typeFolder, $"{name}_Solo");
            ConfigureProfile(soloProfile, settings,
                (pursue, settings.PursueWeight),
                (avoid, settings.AvoidWeight));
        }

        // FSM (opcional)
        AIState initialState = null;

        if (settings.CreateFsm)
        {
            initialState = CreateFsm(settings, sharedFolder, typeFolder,
                pursue, avoid, separation, alignment, cohesion);
        }

        // Arma (opcional)
        GunData gun = null;

        if (settings.CreateGun)
        {
            gun = EditorAssetUtility.LoadOrCreateAsset<GunData>(typeFolder, $"{name}_Gun");
            EditorAssetUtility.SetString(gun, "gunName", name);
            EditorAssetUtility.SetFloat(gun, "damage", settings.Damage);
            EditorAssetUtility.SetFloat(gun, "fireRate", settings.FireRate);
            EditorAssetUtility.SetFloat(gun, "projectileSpeed", settings.ProjectileSpeed);
            EditorAssetUtility.SetFloat(gun, "range", settings.Range);
        }

        AssetDatabase.SaveAssets();

        return new EnemyDataSet(stats, movement, flockProfile, soloProfile, gun, initialState);
    }

    // Material branco partilhado pelo HitFlash de todos os inimigos (<pasta>/Shared/HitFlash_White.mat).
    public static Material LoadOrCreateFlashMaterial(string dataFolder)
    {
        string folder = $"{dataFolder.TrimEnd('/')}/Shared";
        EditorAssetUtility.EnsureFolder(folder);
        string path = $"{folder}/HitFlash_White.mat";

        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogWarning("Enemy Tools: nao encontrei um shader Unlit para o material do HitFlash. Cria um material branco e atribui-o ao HitFlash.");
            return null;
        }

        Material material = new Material(shader) { name = "HitFlash_White" };

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // Patrol (inicial) -> Attack <-> Orbit, e Flee quando a vida esta baixa.
    // Todos os perfis mantem Separation/Alignment/Cohesion: com bando mantem o cardume junto,
    // a solo esses behaviours devolvem zero (vizinhanca vazia), por isso a mesma FSM serve nos dois casos.
    private static AIState CreateFsm(EnemyDataSettings settings, string sharedFolder, string typeFolder,
        PursueTargetBehaviour pursue, AvoidObstaclesBehaviour avoid,
        SeparationBehaviour separation, AlignmentBehaviour alignment, CohesionBehaviour cohesion)
    {
        string name = settings.Name;
        string fsmFolder = $"{typeFolder}/FSM";

        // Behaviours da FSM
        WanderBehaviour wander = EditorAssetUtility.LoadOrCreateAsset<WanderBehaviour>(typeFolder, $"{name}_Wander");
        EditorAssetUtility.SetFloat(wander, "_frequency", settings.WanderFrequency);
        EditorAssetUtility.SetFloat(wander, "_verticalAmount", settings.WanderVertical);

        OrbitTargetBehaviour orbit = EditorAssetUtility.LoadOrCreateAsset<OrbitTargetBehaviour>(typeFolder, $"{name}_Orbit");
        EditorAssetUtility.SetFloat(orbit, "_desiredDistance", settings.OrbitDistance);

        FleeTargetBehaviour flee = EditorAssetUtility.LoadOrCreateAsset<FleeTargetBehaviour>(sharedFolder, "Flee");

        // Perfis por estado
        BehaviourProfile patrolProfile = CreateStateProfile(fsmFolder, $"{name}_PatrolProfile", settings,
            wander, settings.WanderWeight, separation, alignment, cohesion, avoid);
        BehaviourProfile attackProfile = CreateStateProfile(fsmFolder, $"{name}_AttackProfile", settings,
            pursue, settings.PursueWeight, separation, alignment, cohesion, avoid);
        BehaviourProfile orbitProfile = CreateStateProfile(fsmFolder, $"{name}_OrbitProfile", settings,
            orbit, settings.OrbitWeight, separation, alignment, cohesion, avoid);
        BehaviourProfile fleeProfile = CreateStateProfile(fsmFolder, $"{name}_FleeProfile", settings,
            flee, settings.FleeWeight, separation, alignment, cohesion, avoid);

        // Estados
        AIState patrol = CreateState(fsmFolder, $"{name}_Patrol", patrolProfile, 3f, 6f, settings.CommitTime);
        AIState attack = CreateState(fsmFolder, $"{name}_Attack", attackProfile, 3f, 6f, settings.CommitTime);
        AIState orbitState = CreateState(fsmFolder, $"{name}_OrbitState", orbitProfile,
            settings.OrbitMinDuration, settings.OrbitMaxDuration, settings.CommitTime);
        AIState fleeState = CreateState(fsmFolder, $"{name}_Flee", fleeProfile,
            settings.FleeMinDuration, settings.FleeMaxDuration, settings.CommitTime);

        // Condicoes
        StateTimeUpCondition timeUp = EditorAssetUtility.LoadOrCreateAsset<StateTimeUpCondition>(sharedFolder, "StateTimeUp");

        TargetDistanceCondition detect = CreateDistanceCondition(fsmFolder, $"{name}_Detect",
            ComparisonMode.Below, settings.DetectRange);
        TargetDistanceCondition lose = CreateDistanceCondition(fsmFolder, $"{name}_Lose",
            ComparisonMode.Above, settings.LoseRange);
        TargetDistanceCondition close = CreateDistanceCondition(fsmFolder, $"{name}_Close",
            ComparisonMode.Below, settings.CloseRange);
        TargetDistanceCondition orbitBreak = CreateDistanceCondition(fsmFolder, $"{name}_OrbitBreak",
            ComparisonMode.Above, settings.OrbitBreakRange);

        HealthCondition lowHealth = EditorAssetUtility.LoadOrCreateAsset<HealthCondition>(fsmFolder, $"{name}_LowHealth");
        EditorAssetUtility.SetEnum(lowHealth, "_mode", (int)ComparisonMode.Below);
        EditorAssetUtility.SetFloat(lowHealth, "_fraction", settings.FleeHealthFraction);

        // Transicoes (a primeira que for verdadeira ganha, por isso a fuga vem sempre primeiro)
        SetTransitions(patrol, (detect, attack));
        SetTransitions(attack, (lowHealth, fleeState), (close, orbitState), (lose, patrol));
        SetTransitions(orbitState, (lowHealth, fleeState), (orbitBreak, attack), (timeUp, attack));
        SetTransitions(fleeState, (timeUp, patrol));

        return patrol;
    }

    private static BehaviourProfile CreateStateProfile(string folder, string assetName, EnemyDataSettings settings,
        SteeringBehaviour main, float mainWeight,
        SeparationBehaviour separation, AlignmentBehaviour alignment, CohesionBehaviour cohesion,
        AvoidObstaclesBehaviour avoid)
    {
        BehaviourProfile profile = EditorAssetUtility.LoadOrCreateAsset<BehaviourProfile>(folder, assetName);
        ConfigureProfile(profile, settings,
            (main, mainWeight),
            (separation, settings.SeparationWeight),
            (alignment, settings.AlignmentWeight),
            (cohesion, settings.CohesionWeight),
            (avoid, settings.AvoidWeight));

        return profile;
    }

    private static AIState CreateState(string folder, string assetName, BehaviourProfile profile,
        float minDuration, float maxDuration, float commitTime)
    {
        AIState state = EditorAssetUtility.LoadOrCreateAsset<AIState>(folder, assetName);
        EditorAssetUtility.SetObject(state, "_profile", profile);
        EditorAssetUtility.SetFloat(state, "_minDuration", minDuration);
        EditorAssetUtility.SetFloat(state, "_maxDuration", maxDuration);
        EditorAssetUtility.SetFloat(state, "_commitTime", commitTime);
        return state;
    }

    private static TargetDistanceCondition CreateDistanceCondition(string folder, string assetName,
        ComparisonMode mode, float distance)
    {
        TargetDistanceCondition condition = EditorAssetUtility.LoadOrCreateAsset<TargetDistanceCondition>(folder, assetName);
        EditorAssetUtility.SetEnum(condition, "_mode", (int)mode);
        EditorAssetUtility.SetFloat(condition, "_distance", distance);
        return condition;
    }

    private static void SetTransitions(AIState state, params (StateCondition condition, AIState next)[] transitions)
    {
        SerializedObject serialized = new SerializedObject(state);
        SerializedProperty array = serialized.FindProperty("_transitions");
        array.arraySize = transitions.Length;

        for (int i = 0; i < transitions.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_condition").objectReferenceValue = transitions[i].condition;
            element.FindPropertyRelative("_next").objectReferenceValue = transitions[i].next;
        }

        serialized.ApplyModifiedProperties();
    }

    private static void ConfigureProfile(BehaviourProfile profile, EnemyDataSettings settings,
        params (SteeringBehaviour behaviour, float weight)[] entries)
    {
        EditorAssetUtility.SetFloat(profile, "_neighbourRadius", settings.NeighbourRadius);
        EditorAssetUtility.SetFloat(profile, "_separationRadius", settings.SeparationRadius);

        SerializedObject serialized = new SerializedObject(profile);
        SerializedProperty array = serialized.FindProperty("_entries");
        array.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("_behaviour").objectReferenceValue = entries[i].behaviour;
            element.FindPropertyRelative("_weight").floatValue = entries[i].weight;
        }

        serialized.ApplyModifiedProperties();
    }
}

// Parametros de partida para os assets de um tipo de inimigo.
// O nome e todos os valores sao teus: cada tipo e simplesmente o nome + os numeros que escolheres.
[System.Serializable]
public class EnemyDataSettings
{
    [Header("Identificacao")]
    [SerializeField] private string _name = "NewEnemy";
    [SerializeField] private string _folder = "Assets/Enemies/Data";

    [Header("Stats")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Movimento")]
    [SerializeField] private float _minSpeed = 15f;
    [SerializeField] private float _maxSpeed = 35f;
    [SerializeField] private float _acceleration = 20f;
    [SerializeField] private float _lateralGrip = 100f;
    [SerializeField] private float _maxTurnRate = 100f;

    [Header("Perfis estaticos (sem FSM)")]
    [SerializeField] private bool _createFlockProfile = true;
    [SerializeField] private bool _createSoloProfile = true;
    [SerializeField] private float _neighbourRadius = 60f;
    [SerializeField] private float _separationRadius = 15f;
    [SerializeField] private float _pursueWeight = 2f;
    [SerializeField] private float _separationWeight = 1.5f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 0.8f;
    [SerializeField] private float _avoidWeight = 5f;

    [Header("Behaviours com parametros")]
    [SerializeField] private float _leadFactor = 1f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _avoidDistance = 40f;
    [SerializeField] private float _avoidRadius = 2.5f;

    [Header("FSM (estados e transicoes)")]
    [SerializeField] private bool _createFsm = true;
    [SerializeField, Min(0f)] private float _commitTime = 1f;

    [Header("FSM: pesos por estado")]
    [SerializeField] private float _wanderWeight = 1f;
    [SerializeField] private float _orbitWeight = 2f;
    [SerializeField] private float _fleeWeight = 3f;

    [Header("FSM: Wander (Patrol)")]
    [SerializeField] private float _wanderFrequency = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _wanderVertical = 0.6f;

    [Header("FSM: distancias das transicoes")]
    [SerializeField] private float _detectRange = 300f;
    [SerializeField] private float _loseRange = 500f;
    [SerializeField] private float _closeRange = 40f;
    [SerializeField] private float _orbitDistance = 60f;
    [SerializeField] private float _orbitBreakRange = 120f;

    [Header("FSM: fuga e duracoes")]
    [SerializeField, Range(0f, 1f)] private float _fleeHealthFraction = 0.25f;
    [SerializeField] private float _orbitMinDuration = 3f;
    [SerializeField] private float _orbitMaxDuration = 6f;
    [SerializeField] private float _fleeMinDuration = 4f;
    [SerializeField] private float _fleeMaxDuration = 7f;

    [Header("Feedback")]
    [SerializeField] private bool _hitFlash = true;
    [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.08f;

    [Header("Arma (opcional)")]
    [SerializeField] private bool _createGun;
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _fireRate = 4f;
    [SerializeField] private float _projectileSpeed = 80f;
    [SerializeField] private float _range = 150f;

    public string Name => _name;
    public string Folder => _folder;

    public float MaxHealth => _maxHealth;

    public float MinSpeed => _minSpeed;
    public float MaxSpeed => _maxSpeed;
    public float Acceleration => _acceleration;
    public float LateralGrip => _lateralGrip;
    public float MaxTurnRate => _maxTurnRate;

    public bool CreateFlockProfile => _createFlockProfile;
    public bool CreateSoloProfile => _createSoloProfile;
    public float NeighbourRadius => _neighbourRadius;
    public float SeparationRadius => _separationRadius;
    public float PursueWeight => _pursueWeight;
    public float SeparationWeight => _separationWeight;
    public float AlignmentWeight => _alignmentWeight;
    public float CohesionWeight => _cohesionWeight;
    public float AvoidWeight => _avoidWeight;

    public float LeadFactor => _leadFactor;
    public LayerMask ObstacleMask => _obstacleMask;
    public float AvoidDistance => _avoidDistance;
    public float AvoidRadius => _avoidRadius;

    public bool CreateFsm => _createFsm;
    public float CommitTime => _commitTime;
    public float WanderWeight => _wanderWeight;
    public float OrbitWeight => _orbitWeight;
    public float FleeWeight => _fleeWeight;
    public float WanderFrequency => _wanderFrequency;
    public float WanderVertical => _wanderVertical;
    public float DetectRange => _detectRange;
    public float LoseRange => _loseRange;
    public float CloseRange => _closeRange;
    public float OrbitDistance => _orbitDistance;
    public float OrbitBreakRange => _orbitBreakRange;
    public float FleeHealthFraction => _fleeHealthFraction;
    public float OrbitMinDuration => _orbitMinDuration;
    public float OrbitMaxDuration => _orbitMaxDuration;
    public float FleeMinDuration => _fleeMinDuration;
    public float FleeMaxDuration => _fleeMaxDuration;

    public bool HitFlash => _hitFlash;
    public float HitFlashDuration => _hitFlashDuration;

    public bool CreateGun => _createGun;
    public float Damage => _damage;
    public float FireRate => _fireRate;
    public float ProjectileSpeed => _projectileSpeed;
    public float Range => _range;

    public void SetObstacleMask(LayerMask mask) => _obstacleMask = mask;
}

// Valores de partida para o SpaceshipController e o ShipInputAdapter do jogador.
// Os valores por defeito sao os do proprio SpaceshipController.
[System.Serializable]
public class PlayerMovementSettings
{
    [Header("Nave")]
    [SerializeField, Min(0.1f)] private float _maxSpeed = 15f;
    [SerializeField, Min(0.1f)] private float _enginePower = 30f;
    [SerializeField, Range(0f, 1f)] private float _reverseRatio = 0.5f;
    [SerializeField, Min(0.1f)] private float _throttleResponse = 4f;
    [SerializeField, Min(0f)] private float _lateralGrip = 4f;
    [Tooltip("Liga so se o nariz do modelo apontar para -Z. A camara usa o mesmo valor. " +
             "O ponto de disparo que a tool cria fica em +Z.")]
    [SerializeField] private bool _invertForward;

    [Header("Modo de voo")]
    [Tooltip("Free Reticle: o rato move uma mira livre e a nave persegue-a com inercia. " +
             "Direct Nose: o rato roda a nave diretamente.")]
    [SerializeField] private bool _freeReticle = true;
    [SerializeField, Range(5f, 89f)] private float _reticleConeAngle = 35f;
    [SerializeField, Min(1f)] private float _maxPitchRate = 70f;
    [SerializeField, Min(1f)] private float _maxYawRate = 90f;
    [SerializeField, Min(1f)] private float _turnAcceleration = 300f;
    [SerializeField, Range(0f, 80f)] private float _bankAngle = 35f;
    [SerializeField, Range(0f, 45f)] private float _pitchTiltAngle = 12f;

    [Header("Rato")]
    [SerializeField, Min(0f)] private float _mouseSensitivity = 0.15f;
    [SerializeField] private bool _invertMouseY = true;
    [SerializeField] private bool _invertMouseX;
    [SerializeField, Range(0f, 0.1f)] private float _aimSmoothing = 0.03f;

    [Header("Roll (A/D)")]
    [SerializeField, Min(1f)] private float _maxRollRate = 120f;
    [SerializeField, Min(1f)] private float _rollAcceleration = 600f;

    [Header("Input")]
    [SerializeField] private bool _lockCursor = true;

    public float MaxSpeed => _maxSpeed;
    public float EnginePower => _enginePower;
    public float ReverseRatio => _reverseRatio;
    public float ThrottleResponse => _throttleResponse;
    public float LateralGrip => _lateralGrip;
    public bool InvertForward => _invertForward;

    public bool FreeReticle => _freeReticle;
    public float ReticleConeAngle => _reticleConeAngle;
    public float MaxPitchRate => _maxPitchRate;
    public float MaxYawRate => _maxYawRate;
    public float TurnAcceleration => _turnAcceleration;
    public float BankAngle => _bankAngle;
    public float PitchTiltAngle => _pitchTiltAngle;

    public float MouseSensitivity => _mouseSensitivity;
    public bool InvertMouseY => _invertMouseY;
    public bool InvertMouseX => _invertMouseX;
    public float AimSmoothing => _aimSmoothing;

    public float MaxRollRate => _maxRollRate;
    public float RollAcceleration => _rollAcceleration;

    public bool LockCursor => _lockCursor;
}

// Valores de partida para o CameraFollow que segue o jogador.
// Os valores por defeito sao os do proprio CameraFollow.
[System.Serializable]
public class CameraFollowSettings
{
    [SerializeField, Min(0f)] private float _distance = 10f;
    [SerializeField] private float _height = 3f;
    [Tooltip("Distancia a frente da nave para onde a camara olha.")]
    [SerializeField, Min(0f)] private float _lookAheadDistance = 15f;
    [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.08f;
    [SerializeField, Min(0.1f)] private float _rotationSharpness = 8f;

    public float Distance => _distance;
    public float Height => _height;
    public float LookAheadDistance => _lookAheadDistance;
    public float PositionSmoothTime => _positionSmoothTime;
    public float RotationSharpness => _rotationSharpness;
}

// Parametros de partida para o SatelliteRing de um prefab de satelite (usados pela janela Ship Creator).
[System.Serializable]
public class SatelliteSettings
{
    [Header("Zona de passagem")]
    [Tooltip("Calcula o raio interior a partir do tamanho do modelo. Desliga para usar o valor fixo abaixo.")]
    [SerializeField] private bool _autoRadius = true;
    [Tooltip("Fracao do raio do modelo (o menor entre largura e altura) usada como raio interior. " +
             "Mais pequeno = a passagem exige ir mais ao centro.")]
    [SerializeField, Range(0.1f, 1f)] private float _autoRadiusFactor = 0.6f;
    [Tooltip("Raio fixo, em unidades locais (so usado com o raio automatico desligado).")]
    [SerializeField, Min(0.1f)] private float _innerRadius = 20f;
    [Tooltip("Ligado: o aro so pode ser usado uma vez.")]
    [SerializeField] private bool _oneShot;
    [Tooltip("Segundos ate o aro poder ser usado de novo (ignorado se One Shot).")]
    [SerializeField, Min(0f)] private float _cooldown = 5f;

    [Header("Boost")]
    [SerializeField, Min(1f)] private float _speedMultiplier = 3f;
    [SerializeField, Min(0.1f)] private float _boostDuration = 4f;

    [Header("Escudo")]
    [SerializeField, Min(0f)] private float _shieldAmount = 50f;
    [SerializeField, Min(0.1f)] private float _shieldDuration = 10f;

    [Header("Feedback")]
    [SerializeField] private AudioClip _passClip;

    public bool AutoRadius => _autoRadius;
    public float AutoRadiusFactor => _autoRadiusFactor;
    public float InnerRadius => _innerRadius;
    public bool OneShot => _oneShot;
    public float Cooldown => _cooldown;

    public float SpeedMultiplier => _speedMultiplier;
    public float BoostDuration => _boostDuration;

    public float ShieldAmount => _shieldAmount;
    public float ShieldDuration => _shieldDuration;

    public AudioClip PassClip => _passClip;
}

// Parâmetros de partida para a janela Asteroid Setup.
// O que está aqui é copiado para os asteroides da cena, para os prefabs criados e para a nave.
[System.Serializable]
public class AsteroidSetupSettings
{
    [Header("Onde guardar os prefabs novos")]
    [Tooltip("Pasta que já existe no projeto. O fragmento e o recurso são guardados aqui.")]
    [FolderPopup]
    [SerializeField] private string _folder = "Assets";
    [Tooltip("Subpasta a criar dentro da pasta escolhida. Vazio = guarda diretamente na pasta.")]
    [SerializeField] private string _subfolder = "Asteroids";

    [Header("Prefabs que já tens")]
    [Tooltip("Prefab do asteroide (asset do Project). Recebe o Asteroid, o collider e os valores abaixo, " +
             "e as cópias na cena herdam-nos. Opcional.")]
    [SerializeField] private GameObject _asteroidPrefab;
    [Tooltip("Prefab do player (asset do Project). Recebe o ResourceCollector. Opcional.")]
    [SerializeField] private GameObject _playerPrefab;

    [Header("Asteroides já existentes na cena")]
    [Tooltip("Objeto pai dos asteroides já criados. Sem filtro de nome, cada filho direto é um asteroide.")]
    [SerializeField] private Transform _asteroidsParent;
    [Tooltip("Opcional. Procura em toda a hierarquia do objeto pai (ou da cena, se não houver pai) " +
             "os objetos cujo nome contém este texto, por exemplo \"Asteroid\". " +
             "Serve quando os asteroides não são filhos diretos do pai.")]
    [SerializeField] private string _nameFilter = string.Empty;
    [Tooltip("Liga: o objeto pai ganha um AsteroidField e os valores abaixo passam a ser editados nele " +
             "(valem para todos os asteroides lá dentro). Desliga: os valores são copiados para cada asteroide.")]
    [SerializeField] private bool _controlFromParent = true;
    [Tooltip("Collider a acrescentar aos asteroides (e fragmentos) que ainda não têm nenhum.")]
    [SerializeField] private ShipColliderType _colliderType = ShipColliderType.Sphere;
    [SerializeField, Min(1f)] private float _health = 100f;
    [Tooltip("Quantas gerações de fragmentos o asteroide dá. 0 = não larga fragmentos.")]
    [SerializeField, Min(0)] private int _fragmentGenerations = 2;
    [Tooltip("x = mínimo, y = máximo de fragmentos.")]
    [SerializeField] private Vector2Int _fragmentCount = new Vector2Int(3, 5);
    [SerializeField, Range(0.05f, 1f)] private float _fragmentScale = 0.4f;
    [SerializeField, Min(0f)] private float _scatterSpeed = 12f;
    [SerializeField, Min(0f)] private float _maxSpinSpeed = 90f;
    [Tooltip("x = mínimo, y = máximo de recursos largados.")]
    [SerializeField] private Vector2Int _resourceCount = new Vector2Int(1, 2);
    [SerializeField, Range(0f, 1f)] private float _dropChance = 1f;
    [SerializeField, Min(0f)] private float _resourceScatterSpeed = 6f;
    [SerializeField] private AudioClip _breakClip;
    [SerializeField] private ParticleSystem _breakEffect;

    [Header("Feedback de dano (HitFlash)")]
    [Tooltip("Acrescenta o HitFlash aos asteroides e aos fragmentos.")]
    [SerializeField] private bool _hitFlash = true;
    [Tooltip("Material branco do flash. Se já usas um nos inimigos, atribui esse. " +
             "Vazio = cria (ou reutiliza) HitFlash_White na subpasta Shared da pasta de saída.")]
    [SerializeField] private Material _hitFlashMaterial;
    [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.08f;

    [Header("Fragmentos")]
    [Tooltip("Vazio = copia a malha e o material do primeiro asteroide encontrado. " +
             "Se atribuíres um, usa o mesmo modelo dos asteroides, para a escala bater certo. " +
             "O fragmento recebe a layer do primeiro asteroide.")]
    [SerializeField] private GameObject _fragmentModel;
    [SerializeField, Min(1f)] private float _fragmentHealth = 30f;
    [Tooltip("Segundos até o fragmento desaparecer sem se partir. 0 = nunca.")]
    [SerializeField, Min(0f)] private float _fragmentLifetime = 0f;
    [Tooltip("x = mínimo, y = máximo de recursos que cada fragmento larga.")]
    [SerializeField] private Vector2Int _fragmentResourceCount = new Vector2Int(1, 1);
    [SerializeField, Range(0f, 1f)] private float _fragmentDropChance = 0.25f;

    [Header("Recurso")]
    [Tooltip("Vazio = cria uma esfera simples.")]
    [SerializeField] private GameObject _resourceModel;
    [Tooltip("Vazio = fica com o material do modelo.")]
    [SerializeField] private Material _resourceMaterial;
    [SerializeField, Min(0.01f)] private float _resourceScale = 1f;
    [SerializeField, Min(0f)] private float _resourceDrag = 1.5f;
    [Tooltip("Segundos até desaparecer se ninguém o apanhar. 0 = nunca.")]
    [SerializeField, Min(0f)] private float _resourceLifetime = 30f;

    [Header("Pool")]
    [SerializeField, Min(0)] private int _fragmentInitialCapacity = 30;
    [SerializeField, Min(1)] private int _fragmentMaxCapacity = 300;
    [SerializeField, Min(0)] private int _fragmentPrewarm = 30;
    [SerializeField, Min(0)] private int _resourceInitialCapacity = 20;
    [SerializeField, Min(1)] private int _resourceMaxCapacity = 200;
    [SerializeField, Min(0)] private int _resourcePrewarm = 20;

    [Header("Nave do jogador")]
    [Tooltip("Objeto da nave na cena. Recebe o ResourceCollector.")]
    [SerializeField] private GameObject _ship;
    [Tooltip("Ponto na traseira da nave. Vazio = centro da nave.")]
    [SerializeField] private Transform _attachPoint;
    [Tooltip("Liga se o nariz do modelo apontar para -Z (igual ao SpaceshipController).")]
    [SerializeField] private bool _invertForward = true;
    [SerializeField, Min(0.1f)] private float _pickupRadius = 8f;
    [SerializeField, Min(1)] private int _maxCarried = 20;
    [SerializeField, Min(0.1f)] private float _spacing = 4f;
    [SerializeField, Min(0.1f)] private float _stiffness = 20f;
    [SerializeField, Min(0f)] private float _damping = 5f;
    [SerializeField, Min(1f)] private float _maxStretch = 1.6f;
    [SerializeField] private AudioClip _collectClip;

    [Header("Planetas (entrega com E)")]
    [Tooltip("Quantos recursos cada planeta aceita no total.")]
    [SerializeField, Min(1)] private int _planetCapacity = 10;
    [Tooltip("A nave pode entregar a esta distancia da superficie (raio do planeta + margem).")]
    [SerializeField, Min(0f)] private float _deliveryMargin = 30f;
    [Tooltip("Opcional. Objeto de UI mostrado quando se pode entregar (ex.: 'E - Entregar').")]
    [SerializeField] private GameObject _deliverPrompt;
    [SerializeField] private AudioClip _deliverClip;

    [Header("Projétil (opcional)")]
    [Tooltip("Prefab do projétil. Se atribuído, as layers dos asteroides são acrescentadas ao hitMask dele.")]
    [SerializeField] private GameObject _projectilePrefab;

    // Pasta escolhida mais a subpasta (se houver).
    public string OutputFolder
    {
        get
        {
            string baseFolder = string.IsNullOrWhiteSpace(_folder) ? "Assets" : _folder.TrimEnd('/');
            string sub = _subfolder == null ? string.Empty : _subfolder.Trim().Trim('/');

            return sub.Length == 0 ? baseFolder : $"{baseFolder}/{sub}";
        }
    }

    public GameObject AsteroidPrefab => _asteroidPrefab;
    public GameObject PlayerPrefab => _playerPrefab;

    public Transform AsteroidsParent => _asteroidsParent;
    public string NameFilter => _nameFilter;
    public bool ControlFromParent => _controlFromParent;
    public ShipColliderType ColliderType => _colliderType;
    public float Health => _health;
    public int FragmentGenerations => _fragmentGenerations;
    public Vector2Int FragmentCount => _fragmentCount;
    public float FragmentScale => _fragmentScale;
    public float ScatterSpeed => _scatterSpeed;
    public float MaxSpinSpeed => _maxSpinSpeed;
    public Vector2Int ResourceCount => _resourceCount;
    public float DropChance => _dropChance;
    public float ResourceScatterSpeed => _resourceScatterSpeed;
    public AudioClip BreakClip => _breakClip;
    public ParticleSystem BreakEffect => _breakEffect;

    public bool HitFlash => _hitFlash;
    public Material HitFlashMaterial => _hitFlashMaterial;
    public float HitFlashDuration => _hitFlashDuration;

    public GameObject FragmentModel => _fragmentModel;
    public float FragmentHealth => _fragmentHealth;
    public float FragmentLifetime => _fragmentLifetime;
    public Vector2Int FragmentResourceCount => _fragmentResourceCount;
    public float FragmentDropChance => _fragmentDropChance;

    public GameObject ResourceModel => _resourceModel;
    public Material ResourceMaterial => _resourceMaterial;
    public float ResourceScale => _resourceScale;
    public float ResourceDrag => _resourceDrag;
    public float ResourceLifetime => _resourceLifetime;

    public int FragmentInitialCapacity => _fragmentInitialCapacity;
    public int FragmentMaxCapacity => _fragmentMaxCapacity;
    public int FragmentPrewarm => _fragmentPrewarm;
    public int ResourceInitialCapacity => _resourceInitialCapacity;
    public int ResourceMaxCapacity => _resourceMaxCapacity;
    public int ResourcePrewarm => _resourcePrewarm;

    public GameObject Ship => _ship;
    public Transform AttachPoint => _attachPoint;
    public bool InvertForward => _invertForward;
    public float PickupRadius => _pickupRadius;
    public int MaxCarried => _maxCarried;
    public float Spacing => _spacing;
    public float Stiffness => _stiffness;
    public float Damping => _damping;
    public float MaxStretch => _maxStretch;
    public AudioClip CollectClip => _collectClip;

    public int PlanetCapacity => _planetCapacity;
    public float DeliveryMargin => _deliveryMargin;
    public GameObject DeliverPrompt => _deliverPrompt;
    public AudioClip DeliverClip => _deliverClip;

    public GameObject ProjectilePrefab => _projectilePrefab;
}

// Parametros de partida para a janela Planet Setup.
// Servem para planetas que ja estao na cena: nao e preciso criar nenhum prefab.
[System.Serializable]
public class PlanetSetupSettings
{
    [Header("Planetas na cena")]
    [Tooltip("Objeto pai dos planetas. Sem filtro de nome, cada filho direto e um planeta.")]
    [SerializeField] private Transform _planetsParent;
    [Tooltip("Opcional. Procura, em toda a hierarquia do objeto pai (ou da cena, se nao houver pai), " +
             "os objetos cujo nome contem este texto, por exemplo \"Planet\".")]
    [SerializeField] private string _nameFilter = "Planet";

    [Header("Entrega")]
    [Tooltip("Quantos recursos cada planeta aceita no total.")]
    [SerializeField, Min(1)] private int _capacity = 10;
    [Tooltip("Ligado: o raio de entrega e calculado pelo tamanho do planeta (raio + margem). " +
             "Desligado: usa o raio fixo.")]
    [SerializeField] private bool _autoRadius = true;
    [Tooltip("A nave pode entregar a esta distancia da superficie (so com o raio automatico).")]
    [SerializeField, Min(0f)] private float _deliveryMargin = 30f;
    [Tooltip("Raio fixo, em unidades do mundo (so com o raio automatico desligado).")]
    [SerializeField, Min(0.1f)] private float _fixedRadius = 60f;
    [Tooltip("Ligado: os planetas que ja tem ResourcePlanet tambem recebem estes valores. " +
             "Desligado: so os planetas novos sao configurados e os teus valores nao sao mexidos.")]
    [SerializeField] private bool _overwriteExisting;

    [Header("Nave do jogador (opcional)")]
    [Tooltip("Objeto da nave na cena. Recebe o ResourceDelivery (precisa de um ResourceCollector, " +
             "que o Asteroid Setup acrescenta).")]
    [SerializeField] private GameObject _ship;
    [Tooltip("Opcional. Objeto de UI mostrado quando se pode entregar (ex.: 'E - Entregar').")]
    [SerializeField] private GameObject _deliverPrompt;
    [SerializeField] private AudioClip _deliverClip;

    public Transform PlanetsParent => _planetsParent;
    public string NameFilter => _nameFilter;

    public int Capacity => _capacity;
    public bool AutoRadius => _autoRadius;
    public float DeliveryMargin => _deliveryMargin;
    public float FixedRadius => _fixedRadius;
    public bool OverwriteExisting => _overwriteExisting;

    public GameObject Ship => _ship;
    public GameObject DeliverPrompt => _deliverPrompt;
    public AudioClip DeliverClip => _deliverClip;
}
