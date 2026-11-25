using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Game Tools
// Uma unica janela com separadores: Ship Creator, Asteroid Setup e Planet Setup.
// As tres janelas (ShipCreatorWindow, AsteroidSetupWindow, PlanetSetupWindow) estao neste ficheiro.
// Colocar numa pasta Editor.
public class GameToolsWindow : EditorWindow
{
    private static readonly System.Type[] TabTypes =
    {
        typeof(ShipCreatorWindow),
        typeof(AsteroidSetupWindow),
        typeof(PlanetSetupWindow),
    };

    private static readonly string[] TabNames = { "Ship Creator", "Asteroid Setup", "Planet Setup" };

    [SerializeField] private int _tab;

    private EditorWindow[] _hosted;
    private System.Reflection.MethodInfo[] _onGui;

    [MenuItem("Tools/Game Tools")]
    private static void Open()
    {
        GameToolsWindow window = GetWindow<GameToolsWindow>("Game Tools");
        window.minSize = new Vector2(460f, 560f);
    }

    private void OnEnable()
    {
        minSize = new Vector2(460f, 560f);
        EnsureHosted();
    }

    private void OnDisable()
    {
        if (_hosted == null)
            return;

        foreach (EditorWindow window in _hosted)
        {
            if (window != null)
                DestroyImmediate(window);
        }

        _hosted = null;
    }

    private void EnsureHosted()
    {
        if (_hosted != null && _hosted.Length == TabTypes.Length)
        {
            bool allAlive = true;

            foreach (EditorWindow window in _hosted)
                allAlive &= window != null;

            if (allAlive)
                return;
        }

        _hosted = new EditorWindow[TabTypes.Length];
        _onGui = new System.Reflection.MethodInfo[TabTypes.Length];

        for (int i = 0; i < TabTypes.Length; i++)
        {
            // Instância "escondida": nunca abre como janela, só serve para desenhar o separador.
            EditorWindow window = (EditorWindow)CreateInstance(TabTypes[i]);
            // HideAndDontSave inclui NotEditable, o que deixava todos os campos do separador cinzentos.
            window.hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;

            _hosted[i] = window;
            _onGui[i] = TabTypes[i].GetMethod("OnGUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        }
    }

    private void OnGUI()
    {
        EnsureHosted();

        _tab = Mathf.Clamp(GUILayout.Toolbar(_tab, TabNames, GUILayout.Height(26)), 0, TabTypes.Length - 1);
        EditorGUILayout.Space(2);

        System.Reflection.MethodInfo method = _onGui[_tab];
        EditorWindow hosted = _hosted[_tab];

        if (method == null || hosted == null)
        {
            EditorGUILayout.HelpBox($"Não consegui desenhar '{TabNames[_tab]}'.", MessageType.Error);
            return;
        }

        try
        {
            method.Invoke(hosted, null);
        }
        catch (System.Reflection.TargetInvocationException exception)
        {
            // GUIUtility.ExitGUI() (usado pelos botões) tem de chegar ao Unity intacto.
            if (exception.InnerException is ExitGUIException)
                throw exception.InnerException;

            Debug.LogException(exception.InnerException ?? exception);
        }
    }

    // As janelas escondidas pedem Repaint a si próprias; isto garante que esta também redesenha.
    private void OnInspectorUpdate()
    {
        Repaint();
    }
}

// ShipCreatorWindow.cs
// Colocar em Assets/Editor/ (a pasta tem de se chamar "Editor").
// Abre em: Tools > Ship Creator
//
// Uma so janela para:
//   Inimigo : criar/atualizar todos os dados (stats, movimento, behaviours, perfis, FSM de estados, arma) + prefab
//   Jogador : criar/atualizar o prefab do jogador (com movimento e input ja configurados)
//   Satelite: criar/atualizar o prefab do satelite (aro que da boost de velocidade e escudo ao jogador)
//   Cena    : criar pools de projeteis, colocar o jogador (com a camara a segui-lo), os satelites e os inimigos (individuais ou em bando)
//
// O separador Jogador tambem configura o boost/escudo (ShipBoost, PlayerShield) e o efeito FTL da camara (CameraBoostEffect).
//   Sistemas: liga a mira (AimPositioner), o lock de alvo com aim assist (TargetLockSystem + AimAssist), o radar (RadarUI)
//             e a pontuacao (ScoreManager + ScoreUI); tambem poe RadarTarget nos inimigos e liga o input de disparo do jogador.
//
// Depende de: EditorAssetUtility, EnemyDataFactory, EnemyDataSettings, ShipPrefabUtility,
// PlayerMovementSettings, CameraFollowSettings, SatelliteSettings (+ SpaceshipController, ShipInputAdapter, CameraFollow,
// SatelliteRing, ShipBoost, PlayerShield, CameraBoostEffect, AimPositioner, TargetLockSystem, AimAssist, RadarUI,
// RadarTarget, ScoreManager, ScoreUI)
// (substitui o EnemyCreatorWindow, que podes apagar).
public class ShipCreatorWindow : EditorWindow
{
    private enum Tab { Enemy, Player, Satellite, Scene, Systems }
    private enum ProfileChoice { Flock, Solo }
    private enum SpawnMode { Individual, Flock }

    [System.Serializable]
    private class SpawnEntry
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count = 5;

        public GameObject Prefab { get => _prefab; set => _prefab = value; }
        public int Count { get => _count; set => _count = value; }
    }

    private const string PlayerLayerName = "PlayerShip";
    private const string EnemyLayerName = "EnemyShip";
    private const string ObstacleLayerName = "Obstacle";

    private SerializedObject _so;

    [SerializeField] private Tab _tab;
    [SerializeField] private Vector2 _scroll;

    // ---- Layers
    [SerializeField] private int _playerLayer;
    [SerializeField] private int _enemyLayer;
    [SerializeField] private int _obstacleLayer;

    // ---- Inimigo: dados
    [SerializeField] private EnemyDataSettings _settings = new EnemyDataSettings();
    [SerializeField] private ShipStats _enemyStats;
    [SerializeField] private MovementProfile _enemyMovement;
    [SerializeField] private BehaviourProfile _flockProfile;
    [SerializeField] private BehaviourProfile _soloProfile;
    [SerializeField] private GunData _enemyGun;
    [SerializeField] private AIState _initialState;

    // ---- Inimigo: prefab
    [SerializeField] private GameObject _enemyModel;
    [SerializeField] private ShipColliderType _enemyCollider = ShipColliderType.Box;
    [SerializeField] private ProfileChoice _profileChoice = ProfileChoice.Flock;
    [SerializeField] private bool _useFsm = true;
    [FolderPopup]
    [SerializeField] private string _enemyPrefabFolder = "Assets/Enemies";

    // ---- Jogador
    [SerializeField] private string _playerName = "Player";
    [FolderPopup]
    [SerializeField] private string _playerFolder = "Assets/Player";
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private ShipColliderType _playerCollider = ShipColliderType.Box;
    [SerializeField] private bool _playerRigidbody = true;
    [SerializeField] private float _playerMaxHealth = 100f;
    [SerializeField] private ShipStats _playerStats;
    [SerializeField] private GunData _playerGun;
    [SerializeField] private GunData _playerSecondGun;
    [SerializeField, Min(0.1f)] private float _playerGunSpacing = 2f;
    [SerializeField] private AudioClip _playerShootClip;
    [SerializeField] private AudioClip _playerDeathClip;
    [SerializeField] private List<MonoScript> _playerExtraScripts = new List<MonoScript>();

    // ---- Jogador: movimento, input e camara
    [SerializeField] private bool _playerMovement = true;
    [SerializeField] private PlayerMovementSettings _movement = new PlayerMovementSettings();
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private bool _setupCameraOnPlace = true;
    [SerializeField] private CameraFollowSettings _cameraSettings = new CameraFollowSettings();

    // ---- Jogador: boost e escudo (satelites)
    [SerializeField] private bool _playerBoostShield = true;
    [SerializeField, Min(0.01f)] private float _boostRampIn = 0.5f;
    [SerializeField, Min(0.01f)] private float _boostRampOut = 1.5f;
    [SerializeField] private Material _shieldVisualMaterial;

    // ---- Camara: efeito FTL durante o boost
    [SerializeField] private bool _cameraBoostEffect = true;
    [SerializeField, Min(0f)] private float _boostFovIncrease = 35f;
    [SerializeField, Min(0f)] private float _boostPunchFov = 15f;
    [SerializeField, Min(0f)] private float _boostPullBack = 6f;

    // ---- Opcoes comuns de prefab
    [SerializeField] private bool _replaceModel;
    [SerializeField] private bool _rebuildCollider;

    // ---- Inimigo existente (prefab ou objeto da cena) a quem se aplica a IA nova
    [SerializeField] private GameObject _existingEnemy;

    // ---- Inimigo: radar / lock de alvo
    [SerializeField] private bool _enemyRadarTarget = true;

    // ---- Jogador: input da arma
    [SerializeField] private InputActionReference _fireAction;
    [SerializeField] private InputActionReference _reloadAction;

    // ---- Sistemas: mira, lock de alvo, radar
    [FolderPopup]
    [SerializeField] private string _hudFolder = "Assets/UI";
    [SerializeField] private Sprite _crosshairSprite;
    [SerializeField] private Vector2 _crosshairSize = new Vector2(32f, 32f);
    [SerializeField, Min(1f)] private float _aimDistance = 100f;
    [SerializeField, Range(0.02f, 1f)] private float _lockSelectionRadius = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _lockCameraFocusBlend = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _aimAssistLevel = 0.5f;
    [SerializeField, Min(1f)] private float _radarRange = 300f;
    [SerializeField, Min(40f)] private float _radarSize = 180f;

    // ---- Cena: pools
    [SerializeField] private GameObject _playerProjectilePrefab;
    [SerializeField] private LayerMask _playerHitMask;
    [SerializeField] private ProjectilePool _playerPool;
    [SerializeField] private GameObject _enemyProjectilePrefab;
    [SerializeField] private LayerMask _enemyHitMask;
    [SerializeField] private ProjectilePool _enemyPool;
    [SerializeField] private int _poolInitialCapacity = 20;
    [SerializeField] private int _poolMaxCapacity = 200;
    [SerializeField] private bool _applyHitMask = true;

    // ---- Cena: jogador e inimigos
    [SerializeField] private GameObject _playerPrefabToPlace;
    [SerializeField] private SpawnMode _mode = SpawnMode.Flock;
    [SerializeField] private List<SpawnEntry> _entries = new List<SpawnEntry>();
    [SerializeField] private Transform _target;
    [SerializeField] private Flock _existingFlock;
    [SerializeField] private float _spreadRadius = 30f;

    // ---- Satelite: prefab
    [SerializeField] private string _satelliteName = "Satellite";
    [FolderPopup]
    [SerializeField] private string _satelliteFolder = "Assets/Satellites";
    [SerializeField] private GameObject _satelliteModel;
    [SerializeField] private Vector3 _satelliteModelEuler;
    [SerializeField] private bool _satelliteMeshCollider = true;
    [SerializeField] private int _satelliteLayer;
    [SerializeField] private SatelliteSettings _satellite = new SatelliteSettings();

    // ---- Cena: satelites
    [SerializeField] private GameObject _satellitePrefabToPlace;
    [SerializeField, Min(1)] private int _satelliteCount = 3;
    [SerializeField, Min(0f)] private float _satelliteSpread = 300f;
    [SerializeField, Min(0f)] private float _satelliteMinSpacing = 80f;
    [SerializeField, Min(0.1f)] private float _satelliteMinScale = 1f;
    [SerializeField, Min(0.1f)] private float _satelliteMaxScale = 1f;
    [SerializeField] private bool _satelliteFacePlayer = true;

    private void OnEnable()
    {
        minSize = new Vector2(440f, 520f);
        FindInputActions(false);
        _so = new SerializedObject(this);
        _so.FindProperty("_settings").isExpanded = true;
        _so.FindProperty("_satellite").isExpanded = true;
        DetectLayers();
    }

    private void DetectLayers()
    {
        if (_playerLayer == 0) _playerLayer = Mathf.Max(0, LayerMask.NameToLayer(PlayerLayerName));
        if (_enemyLayer == 0) _enemyLayer = Mathf.Max(0, LayerMask.NameToLayer(EnemyLayerName));
        if (_obstacleLayer == 0) _obstacleLayer = Mathf.Max(0, LayerMask.NameToLayer(ObstacleLayerName));
        if (_satelliteLayer == 0) _satelliteLayer = _obstacleLayer;
    }

    // =============================================================== GUI

    private void OnGUI()
    {
        // Garante que um estado "desativado" deixado por um erro anterior nao bloqueia a janela toda.
        GUI.enabled = true;

        if (_so == null) _so = new SerializedObject(this);
        _so.Update();

        DrawHeader();

        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Inimigo", "Jogador", "Satelite", "Cena", "Sistemas" });
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        switch (_tab)
        {
            case Tab.Enemy: DrawEnemyTab(); break;
            case Tab.Player: DrawPlayerTab(); break;
            case Tab.Satellite: DrawSatelliteTab(); break;
            case Tab.Systems: DrawSystemsTab(); break;
            default: DrawSceneTab(); break;
        }

        EditorGUILayout.EndScrollView();

        _so.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(
                $"Layers: Player = {LayerLabel(_playerLayer)}  |  Enemy = {LayerLabel(_enemyLayer)}  |  Obstacle = {LayerLabel(_obstacleLayer)}",
                EditorStyles.miniLabel);

            if (GUILayout.Button("Preparar layers", GUILayout.Width(110)))
            {
                SetupLayers();
                GUIUtility.ExitGUI();
            }
        }
    }

    private static string LayerLabel(int layer) =>
        layer > 0 ? LayerMask.LayerToName(layer) : "(nao definida)";

    private void Prop(string field, string label = null)
    {
        SerializedProperty property = _so.FindProperty(field);
        if (label == null)
            EditorGUILayout.PropertyField(property, true);
        else
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    // =============================================================== Inimigo

    private void DrawEnemyTab()
    {
        EditorGUILayout.LabelField("Atualizar a IA de um inimigo que ja existe", EditorStyles.boldLabel);
        Prop("_existingEnemy", "Inimigo (arrasta o prefab ou um da cena)");
        EditorGUILayout.HelpBox(
            "Arrasta o prefab ou escolhe um inimigo da cena. O tool usa o nome dele, refaz so a FSM (estados, perfis, " +
            "condicoes) com os valores dos Parametros e liga-a ao inimigo. Stats, movimento e arma ficam como estao. " +
            "Os pesos e duracoes da FSM sao reescritos com os valores dos Parametros.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(_existingEnemy == null))
        {
            if (GUILayout.Button("Aplicar IA nova ao inimigo", GUILayout.Height(28)))
            {
                UpgradeEnemyAi();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ou criar um inimigo de raiz", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("1. Dados do inimigo", EditorStyles.boldLabel);

        Prop("_settings", "Parametros");

        if (GUILayout.Button("Criar / atualizar dados", GUILayout.Height(28)))
        {
            CreateEnemyData();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.HelpBox(
            "Se um asset com o mesmo nome ja existir, e reaproveitado e os valores sao atualizados. " +
            "Separation, Alignment, Cohesion, Flee e StateTimeUp ficam em <pasta>/Shared e sao partilhados. " +
            "Os estados, perfis por estado e condicoes da FSM ficam em <pasta>/<nome>/FSM.",
            MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Dados criados (podes trocar por outros)", EditorStyles.boldLabel);
        Prop("_enemyStats", "Stats");
        Prop("_enemyMovement", "Movement Profile");
        Prop("_flockProfile", "Behaviour (Flock)");
        Prop("_soloProfile", "Behaviour (Solo)");
        Prop("_initialState", "Estado inicial (FSM)");
        Prop("_enemyGun", "Arma (opcional)");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Prefab do inimigo", EditorStyles.boldLabel);
        Prop("_enemyModel", "Modelo");
        Prop("_enemyCollider", "Collider");
        _enemyLayer = EditorGUILayout.LayerField("Layer", _enemyLayer);
        Prop("_useFsm", "Usar FSM (igual em bando e a solo)");

        if (!_useFsm)
            Prop("_profileChoice", "Perfil a usar");

        Prop("_enemyRadarTarget", "Radar / lock de alvo (RadarTarget)");
        Prop("_enemyPrefabFolder", "Pasta do prefab");
        Prop("_replaceModel", "Substituir modelo (se o prefab ja existir)");
        Prop("_rebuildCollider", "Refazer collider (se o prefab ja existir)");

        string error = ValidateEnemyPrefab();
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else if (_enemyLayer == 0)
            EditorGUILayout.HelpBox("O inimigo vai ficar na layer Default. Usa 'Preparar layers' e escolhe EnemyShip.", MessageType.Info);

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Criar / atualizar prefab", GUILayout.Height(28)))
            {
                BuildEnemyPrefab();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Fazer tudo (dados + prefab)", GUILayout.Height(32)))
        {
            CreateEnemyData();

            if (ValidateEnemyPrefab() == null)
                BuildEnemyPrefab();

            GUIUtility.ExitGUI();
        }
    }

    private void UpgradeEnemyAi()
    {
        _so.ApplyModifiedProperties();

        GameObject target = _existingEnemy;

        if (target == null)
            return;

        // Prefab (asset), instancia de prefab na cena (edita-se o prefab de origem) ou objeto solto da cena.
        string prefabPath = null;

        if (EditorUtility.IsPersistent(target))
        {
            prefabPath = AssetDatabase.GetAssetPath(target);
        }
        else
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(target);

            if (source != null)
                prefabPath = AssetDatabase.GetAssetPath(source);
        }

        GameObject root = prefabPath != null ? PrefabUtility.LoadPrefabContents(prefabPath) : target;

        try
        {
            SteeringAgent agent = root.GetComponent<SteeringAgent>();

            if (agent == null)
            {
                Debug.LogWarning($"Ship Creator: '{root.name}' nao tem SteeringAgent, por isso nao parece um inimigo.");
                return;
            }

            string enemyName = prefabPath != null ? System.IO.Path.GetFileNameWithoutExtension(prefabPath) : root.name;

            // Pasta dos dados: sobe a partir de um asset do inimigo ate encontrar a pasta com o nome dele.
            string dataFolder = _settings.Folder;
            BehaviourProfile current = new SerializedObject(agent).FindProperty("_behaviour").objectReferenceValue as BehaviourProfile;
            string assetPath = current != null ? AssetDatabase.GetAssetPath(current) : null;

            while (!string.IsNullOrEmpty(assetPath))
            {
                string parent = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');

                if (string.IsNullOrEmpty(parent) || parent == "Assets")
                    break;

                if (System.IO.Path.GetFileName(parent) == enemyName)
                {
                    dataFolder = System.IO.Path.GetDirectoryName(parent).Replace('\\', '/');
                    break;
                }

                assetPath = parent;
            }

            _so.FindProperty("_settings._name").stringValue = enemyName;
            _so.FindProperty("_settings._folder").stringValue = dataFolder;
            _so.ApplyModifiedProperties();

            AIState initialState = EnemyDataFactory.CreateAi(_settings);

            if (initialState == null)
            {
                Debug.LogWarning("Ship Creator: liga 'Criar FSM' nos Parametros para atualizar a IA.");
                return;
            }

            _initialState = initialState;

            EnemyHealth health = root.GetComponent<EnemyHealth>();
            AIFSM fsm = GetOrAdd<AIFSM>(root);
            EditorAssetUtility.SetObject(fsm, "_initialState", initialState);

            if (health != null)
                EditorAssetUtility.SetObject(fsm, "_health", health);

            EditorAssetUtility.SetObject(agent, "_behaviour", initialState.Profile);

            // Liga o gatilho de tiro se o inimigo tiver arma mas ainda nao tiver o AIGunTrigger.
            GunController gun = root.GetComponent<GunController>();

            if (gun != null)
            {
                AIGunTrigger trigger = GetOrAdd<AIGunTrigger>(root);
                EditorAssetUtility.SetObject(trigger, "_gun", gun);
                EditorAssetUtility.SetObject(trigger, "_agent", agent);
            }

            if (prefabPath != null)
            {
                RemoveMissingScripts(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            else
            {
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(root.scene);
            }

            _so.Update();
            Debug.Log($"Ship Creator: IA nova aplicada a '{enemyName}' (dados em {dataFolder}/{enemyName}/FSM).", initialState);
        }
        finally
        {
            if (prefabPath != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private void CreateEnemyData()
    {
        _so.ApplyModifiedProperties();

        if (string.IsNullOrWhiteSpace(_settings.Name))
        {
            Debug.LogWarning("Ship Creator: da um nome ao inimigo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.Folder) || !_settings.Folder.StartsWith("Assets"))
        {
            Debug.LogWarning("Ship Creator: a pasta dos dados tem de comecar em 'Assets'.");
            return;
        }

        EnemyDataSet set = EnemyDataFactory.Create(_settings);

        _enemyStats = set.Stats;
        _enemyMovement = set.Movement;
        _flockProfile = set.FlockProfile;
        _soloProfile = set.SoloProfile;
        _enemyGun = set.Gun;
        _initialState = set.InitialState;

        if (_flockProfile == null && _soloProfile != null) _profileChoice = ProfileChoice.Solo;
        else if (_flockProfile != null) _profileChoice = ProfileChoice.Flock;

        _so.Update();
        Debug.Log($"Ship Creator: dados de '{_settings.Name}' criados/atualizados em {_settings.Folder}.", _enemyStats);
    }

    private BehaviourProfile SelectedProfile() =>
        _profileChoice == ProfileChoice.Flock ? _flockProfile : _soloProfile;

    // Com FSM, o perfil base do agente e o do estado inicial (os estados sem perfil proprio voltam a ele).
    private BehaviourProfile BaseProfile() =>
        _useFsm && _initialState != null && _initialState.Profile != null ? _initialState.Profile : SelectedProfile();

    private string ValidateEnemyPrefab()
    {
        if (string.IsNullOrWhiteSpace(_settings.Name)) return "Da um nome ao inimigo (nos parametros).";
        if (_enemyStats == null) return "Falta o ShipStats (cria os dados primeiro).";
        if (_enemyMovement == null) return "Falta o MovementProfile.";
        if (_useFsm && _initialState == null)
            return "Falta o estado inicial da FSM (cria os dados com 'Criar FSM' ligado, ou desliga 'Usar FSM').";
        if (BaseProfile() == null)
            return _useFsm ? "O estado inicial nao tem Behaviour Profile." : $"Falta o Behaviour Profile ({_profileChoice}).";
        if (string.IsNullOrWhiteSpace(_enemyPrefabFolder) || !_enemyPrefabFolder.StartsWith("Assets"))
            return "A pasta do prefab tem de comecar em 'Assets'.";
        return null;
    }

    private void BuildEnemyPrefab()
    {
        _so.ApplyModifiedProperties();

        string prefabName = _settings.Name;
        GameObject root = LoadOrCreatePrefabContents(prefabName, _enemyModel, _enemyPrefabFolder,
            out string path, out bool created);

        try
        {
            if (!created && _replaceModel)
                ReplaceModel(root, _enemyModel);

            // O SteeringAgent exige o ShipMovementVelocity, que exige o Rigidbody.
            SteeringAgent agent = GetOrAdd<SteeringAgent>(root);
            ShipMovementVelocity movement = GetOrAdd<ShipMovementVelocity>(root);
            Rigidbody body = GetOrAdd<Rigidbody>(root);
            EnemyHealth health = GetOrAdd<EnemyHealth>(root);

            // O RadarTarget e o que o radar e o lock de alvo (botao do meio do rato) enxergam.
            if (_enemyRadarTarget)
            {
                GetOrAdd<RadarTarget>(root);
            }
            else
            {
                RadarTarget existingRadar = root.GetComponent<RadarTarget>();
                if (existingRadar != null)
                    DestroyImmediate(existingRadar);
            }

            ShipPrefabUtility.ConfigureRigidbody(body);
            ApplyCollider(root, _enemyCollider);

            EditorAssetUtility.SetObject(movement, "_profile", _enemyMovement);
            EditorAssetUtility.SetObject(agent, "_behaviour", BaseProfile());
            EditorAssetUtility.SetObject(health, "stats", _enemyStats);

            if (_settings.HitFlash)
            {
                HitFlash flash = GetOrAdd<HitFlash>(root);
                EditorAssetUtility.SetObject(flash, "_health", health);
                EditorAssetUtility.SetObject(flash, "_flashMaterial", EnemyDataFactory.LoadOrCreateFlashMaterial(_settings.Folder));
                EditorAssetUtility.SetFloat(flash, "_duration", _settings.HitFlashDuration);
            }
            else
            {
                HitFlash existingFlash = root.GetComponent<HitFlash>();
                if (existingFlash != null)
                    DestroyImmediate(existingFlash);
            }

            // A mesma FSM serve para bando e individual: quem decide e o _flock do SteeringAgent.
            if (_useFsm)
            {
                AIFSM fsm = GetOrAdd<AIFSM>(root);
                EditorAssetUtility.SetObject(fsm, "_initialState", _initialState);
                EditorAssetUtility.SetObject(fsm, "_health", health);
            }
            else
            {
                AIFSM existingFsm = root.GetComponent<AIFSM>();
                if (existingFsm != null)
                    DestroyImmediate(existingFsm);
            }

            if (_enemyGun != null)
            {
                Transform muzzle = ShipPrefabUtility.GetOrCreateFiringPoint(root);
                GunController gun = GetOrAdd<GunController>(root);
                AIGunTrigger trigger = GetOrAdd<AIGunTrigger>(root);

                EditorAssetUtility.SetObject(gun, "gunData", _enemyGun);
                EditorAssetUtility.SetObject(gun, "firingPoint", muzzle);
                EditorAssetUtility.SetObject(trigger, "_gun", gun);
                EditorAssetUtility.SetObject(trigger, "_agent", agent);
            }

            ShipPrefabUtility.SetLayerRecursively(root, _enemyLayer);

            RemoveMissingScripts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"Ship Creator: prefab do inimigo {(created ? "criado" : "atualizado")} em {path}", prefab);

            AddToSpawnList(prefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        _so.Update();
    }

    private void AddToSpawnList(GameObject prefab)
    {
        foreach (SpawnEntry entry in _entries)
        {
            if (entry.Prefab == prefab)
                return;
        }

        _entries.Add(new SpawnEntry { Prefab = prefab, Count = 5 });
    }

    // =============================================================== Jogador

    private void DrawPlayerTab()
    {
        EditorGUILayout.LabelField("Prefab do jogador", EditorStyles.boldLabel);
        Prop("_playerName", "Nome");
        Prop("_playerFolder", "Pasta");
        Prop("_playerModel", "Modelo");
        Prop("_playerCollider", "Collider");
        _playerLayer = EditorGUILayout.LayerField("Layer", _playerLayer);
        Prop("_playerRigidbody", "Adicionar Rigidbody");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movimento e camara", EditorStyles.boldLabel);
        Prop("_playerMovement", "Adicionar movimento e input (SpaceshipController)");

        if (_playerMovement)
        {
            Prop("_movement", "Parametros de movimento");

            if (_movement.InvertForward)
                EditorGUILayout.HelpBox(
                    "invertForward ligado: a nave voa para -Z e a camara fica do lado +Z, a olhar para -Z. " +
                    "Se o nariz do modelo apontar para +Z, a camara fica a olhar para o lado contrario. " +
                    "O ponto de disparo que a tool cria tambem fica em +Z.",
                    MessageType.Info);
            Prop("_moveAction", "Acao Move (Vector2)");
            Prop("_lookAction", "Acao Look (Vector2)");

            if (GUILayout.Button("Procurar acoes Move / Look"))
            {
                FindInputActions(true);
                _so.Update();
                GUIUtility.ExitGUI();
            }
        }

        Prop("_setupCameraOnPlace", "Configurar a camara ao colocar o jogador na cena");

        if (_setupCameraOnPlace)
        {
            Prop("_cameraSettings", "Parametros da camara");
            Prop("_cameraBoostEffect", "Efeito FTL no boost (CameraBoostEffect)");

            if (_cameraBoostEffect)
            {
                Prop("_boostFovIncrease", "FOV extra no boost (graus)");
                Prop("_boostPunchFov", "Soco de FOV no arranque (graus)");
                Prop("_boostPullBack", "Recuo da camara (m)");
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Boost e escudo (satelites)", EditorStyles.boldLabel);
        Prop("_playerBoostShield", "Adicionar ShipBoost e PlayerShield");

        if (_playerBoostShield)
        {
            if (!_playerMovement)
                EditorGUILayout.HelpBox(
                    "O ShipBoost precisa do SpaceshipController ('Adicionar movimento e input'). Sem ele so o escudo e adicionado.",
                    MessageType.Info);

            Prop("_boostRampIn", "Subida do boost (s)");
            Prop("_boostRampOut", "Descida do boost (s)");
            Prop("_shieldVisualMaterial", "Material do escudo (cria a esfera visual)");

            if (_shieldVisualMaterial == null)
                EditorGUILayout.HelpBox(
                    "Sem material, o escudo funciona mas nao tem esfera visual. Usa um material transparente.",
                    MessageType.None);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Vida", EditorStyles.boldLabel);
        Prop("_playerStats", "Stats");

        using (new EditorGUILayout.HorizontalScope())
        {
            Prop("_playerMaxHealth", "Vida maxima");

            if (GUILayout.Button("Criar Stats", GUILayout.Width(90)))
            {
                CreatePlayerStats();
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Arma (opcional)", EditorStyles.boldLabel);
        Prop("_playerGun", "Arma 1 (Gun Data)");
        Prop("_playerSecondGun", "Arma 2 (opcional)");

        if (_playerSecondGun != null)
        {
            Prop("_playerGunSpacing", "Distancia de cada arma ao centro");
            EditorGUILayout.HelpBox(
                "Com duas armas, o tool cria Gun_Left (arma 1) e Gun_Right (arma 2), cada uma com o seu GunController, " +
                "ponto de disparo e GunAimer. O GunController e o ponto de disparo do centro sao removidos.",
                MessageType.None);
        }

        Prop("_fireAction", "Acao de disparo (Player/Attack)");
        Prop("_reloadAction", "Acao de recarga (Player/Reload)");
        Prop("_playerShootClip", "Som de disparo");
        Prop("_playerDeathClip", "Som de morte");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Scripts extra", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Os teus scripts de movimento / input do jogador. Sao adicionados ao prefab se ainda nao existirem.",
            MessageType.None);
        Prop("_playerExtraScripts", "Scripts");

        EditorGUILayout.Space();
        Prop("_replaceModel", "Substituir modelo (se o prefab ja existir)");
        Prop("_rebuildCollider", "Refazer collider (se o prefab ja existir)");

        string error = ValidatePlayerPrefab();
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else if (_playerLayer == 0)
            EditorGUILayout.HelpBox("O jogador vai ficar na layer Default. Usa 'Preparar layers' e escolhe PlayerShip.", MessageType.Info);

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Criar / atualizar prefab do jogador", GUILayout.Height(30)))
            {
                BuildPlayerPrefab();
                GUIUtility.ExitGUI();
            }
        }
    }

    private string ValidatePlayerPrefab()
    {
        if (string.IsNullOrWhiteSpace(_playerName)) return "Da um nome ao jogador.";
        if (_playerStats == null) return "Falta o ShipStats do jogador (podes criar com 'Criar Stats').";
        if (_playerMovement && (_moveAction == null || _lookAction == null))
            return "Faltam as acoes Move e Look do ShipInputAdapter (usa 'Procurar acoes Move / Look' ou arrasta-as).";
        if (string.IsNullOrWhiteSpace(_playerFolder) || !_playerFolder.StartsWith("Assets"))
            return "A pasta tem de comecar em 'Assets'.";
        return null;
    }

    private void CreatePlayerStats()
    {
        _so.ApplyModifiedProperties();

        string folder = $"{_playerFolder.TrimEnd('/')}/Data";
        ShipStats stats = EditorAssetUtility.LoadOrCreateAsset<ShipStats>(folder, $"{_playerName}_Stats");
        EditorAssetUtility.SetFloat(stats, "_maxHealth", _playerMaxHealth);
        AssetDatabase.SaveAssets();

        _playerStats = stats;
        _so.Update();
        Debug.Log($"Ship Creator: stats do jogador em {folder}.", stats);
    }

    private void BuildPlayerPrefab()
    {
        _so.ApplyModifiedProperties();

        GameObject root = LoadOrCreatePrefabContents(_playerName, _playerModel, _playerFolder,
            out string path, out bool created);

        try
        {
            if (!created && _replaceModel)
                ReplaceModel(root, _playerModel);

            if (_playerRigidbody || _playerMovement)
                ShipPrefabUtility.ConfigureRigidbody(GetOrAdd<Rigidbody>(root));

            ApplyCollider(root, _playerCollider);

            PlayerHealth health = GetOrAdd<PlayerHealth>(root);
            EditorAssetUtility.SetObject(health, "_stats", _playerStats);

            if (_playerDeathClip != null)
                EditorAssetUtility.SetObject(health, "_deathClip", _playerDeathClip);

            if (_playerGun != null)
            {
                // Cada arma da nave tem o seu proprio GunController (com o seu ponto de disparo).
                // So se cria um na raiz, com o ponto central, quando a nave ainda nao tem nenhum.
                List<(GunController controller, GunData data)> guns = new List<(GunController, GunData)>();

                if (_playerSecondGun != null)
                {
                    // Duas armas: uma montagem em cada lado, cada uma com o seu controller e o seu ponto de disparo.
                    GunController centerGun = root.GetComponent<GunController>();
                    if (centerGun != null)
                        DestroyImmediate(centerGun);

                    Transform centerMuzzle = root.transform.Find("FiringPoint");
                    if (centerMuzzle != null)
                        DestroyImmediate(centerMuzzle.gameObject);

                    float muzzleZ = ShipPrefabUtility.TryGetLocalBounds(root, out Bounds shipBounds) ? shipBounds.max.z + 0.5f : 1f;

                    guns.Add((GetOrCreateGunMount(root, "Gun_Left", -_playerGunSpacing, muzzleZ), _playerGun));
                    guns.Add((GetOrCreateGunMount(root, "Gun_Right", _playerGunSpacing, muzzleZ), _playerSecondGun));
                }
                else
                {
                    foreach (GunController existing in root.GetComponentsInChildren<GunController>(true))
                        guns.Add((existing, _playerGun));

                    if (guns.Count == 0)
                    {
                        GunController rootGun = GetOrAdd<GunController>(root);
                        EditorAssetUtility.SetObject(rootGun, "firingPoint", ShipPrefabUtility.GetOrCreateFiringPoint(root));
                        guns.Add((rootGun, _playerGun));
                    }
                }

                AudioSource source = null;

                if (_playerShootClip != null)
                {
                    source = GetOrAdd<AudioSource>(root);
                    source.playOnAwake = false;
                }

                foreach ((GunController gun, GunData gunData) in guns)
                {
                    EditorAssetUtility.SetObject(gun, "gunData", gunData);

                    // Sem estas acoes o GunController do jogador nunca dispara nem recarrega.
                    if (_fireAction != null)
                        EditorAssetUtility.SetObject(gun, "fireAction", _fireAction);

                    if (_reloadAction != null)
                        EditorAssetUtility.SetObject(gun, "reloadAction", _reloadAction);

                    if (source != null)
                    {
                        EditorAssetUtility.SetObject(gun, "audioSource", source);
                        EditorAssetUtility.SetObject(gun, "shootClip", _playerShootClip);
                    }
                }
            }

            if (_playerMovement)
                BuildPlayerMovement(root);

            BuildPlayerBoostShield(root, health);

            AddExtraScripts(root);
            ShipPrefabUtility.SetLayerRecursively(root, _playerLayer);

            RemoveMissingScripts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            _playerPrefabToPlace = prefab;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"Ship Creator: prefab do jogador {(created ? "criado" : "atualizado")} em {path}", prefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        _so.Update();
    }

    // Valores atualizados de cada vez que o prefab e criado/atualizado, tal como nos dados do inimigo.
    // Componentes cujo script foi apagado ou renomeado (ex.: o antigo ReticleGimbal) impedem guardar o prefab.
    // Montagem de uma arma: objeto filho com o seu GunController e um FiringPoint a frente.
    private static GunController GetOrCreateGunMount(GameObject root, string mountName, float x, float muzzleZ)
    {
        Transform mount = root.transform.Find(mountName);

        if (mount == null)
        {
            GameObject mountObject = new GameObject(mountName);
            mountObject.transform.SetParent(root.transform, false);
            mount = mountObject.transform;
        }

        mount.localPosition = new Vector3(x, 0f, 0f);

        Transform muzzle = mount.Find("FiringPoint");

        if (muzzle == null)
        {
            GameObject muzzleObject = new GameObject("FiringPoint");
            muzzleObject.transform.SetParent(mount, false);
            muzzle = muzzleObject.transform;
            muzzle.localPosition = new Vector3(0f, 0f, muzzleZ);
        }

        GunController controller = GetOrAdd<GunController>(mount.gameObject);
        EditorAssetUtility.SetObject(controller, "firingPoint", muzzle);
        return controller;
    }

    private static void RemoveMissingScripts(GameObject root)
    {
        int removed = 0;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);

        if (removed > 0)
            Debug.LogWarning($"Ship Creator: removi {removed} componente(s) com script em falta de '{root.name}'.");
    }

    private void BuildPlayerMovement(GameObject root)
    {
        // O SpaceshipController exige o Rigidbody, que ja foi adicionado acima.
        SpaceshipController ship = GetOrAdd<SpaceshipController>(root);

        EditorAssetUtility.SetFloat(ship, "maxSpeed", _movement.MaxSpeed);
        EditorAssetUtility.SetFloat(ship, "enginePower", _movement.EnginePower);
        EditorAssetUtility.SetFloat(ship, "reverseRatio", _movement.ReverseRatio);
        EditorAssetUtility.SetFloat(ship, "throttleResponse", _movement.ThrottleResponse);
        EditorAssetUtility.SetFloat(ship, "lateralGrip", _movement.LateralGrip);
        EditorAssetUtility.SetBool(ship, "invertForward", _movement.InvertForward);
        EditorAssetUtility.SetFloat(ship, "mouseSensitivity", _movement.MouseSensitivity);
        EditorAssetUtility.SetBool(ship, "invertMouseY", _movement.InvertMouseY);
        EditorAssetUtility.SetBool(ship, "invertMouseX", _movement.InvertMouseX);
        EditorAssetUtility.SetFloat(ship, "aimSmoothing", _movement.AimSmoothing);
        EditorAssetUtility.SetFloat(ship, "maxRollRate", _movement.MaxRollRate);
        EditorAssetUtility.SetFloat(ship, "rollAcceleration", _movement.RollAcceleration);

        // Modo de voo: 0 = DirectNose, 1 = FreeReticle (ordem do enum SpaceshipController.FlightMode).
        EditorAssetUtility.SetEnum(ship, "flightMode", _movement.FreeReticle ? 1 : 0);
        EditorAssetUtility.SetFloat(ship, "reticleConeAngle", _movement.ReticleConeAngle);
        EditorAssetUtility.SetFloat(ship, "maxPitchRate", _movement.MaxPitchRate);
        EditorAssetUtility.SetFloat(ship, "maxYawRate", _movement.MaxYawRate);
        EditorAssetUtility.SetFloat(ship, "turnAcceleration", _movement.TurnAcceleration);
        EditorAssetUtility.SetFloat(ship, "bankAngle", _movement.BankAngle);
        EditorAssetUtility.SetFloat(ship, "pitchTiltAngle", _movement.PitchTiltAngle);
        Transform bankModel = root.transform.Find("Model");
        if (bankModel != null)
            EditorAssetUtility.SetObject(ship, "bankVisual", bankModel);

        // O cano segue a mira livre, para os tiros sairem onde esta o crosshair.
        // Um GunAimer em cada ponto de disparo de cada GunController da nave.
        List<Transform> gimbalMuzzles = new List<Transform>();

        foreach (GunController gunController in root.GetComponentsInChildren<GunController>(true))
        {
            Transform point = new SerializedObject(gunController).FindProperty("firingPoint").objectReferenceValue as Transform;

            if (point != null && !gimbalMuzzles.Contains(point))
                gimbalMuzzles.Add(point);
        }

        foreach (Transform muzzleTransform in gimbalMuzzles)
        {
            GunAimer gimbal = muzzleTransform.GetComponent<GunAimer>();
            if (gimbal == null)
                gimbal = muzzleTransform.gameObject.AddComponent<GunAimer>();
            EditorAssetUtility.SetObject(gimbal, "ship", ship);
        }

        ShipInputAdapter input = GetOrAdd<ShipInputAdapter>(root);
        EditorAssetUtility.SetObject(input, "ship", ship);
        EditorAssetUtility.SetObject(input, "move", _moveAction);
        EditorAssetUtility.SetObject(input, "look", _lookAction);
        EditorAssetUtility.SetBool(input, "lockCursor", _movement.LockCursor);
    }

    // Boost (precisa do SpaceshipController) e escudo. Com a opcao desligada, remove-os do prefab.
    private void BuildPlayerBoostShield(GameObject root, PlayerHealth health)
    {
        ShipBoost existingBoost = root.GetComponent<ShipBoost>();
        PlayerShield existingShield = root.GetComponent<PlayerShield>();

        if (!_playerBoostShield)
        {
            if (existingBoost != null)
                DestroyImmediate(existingBoost);

            EditorAssetUtility.SetObject(health, "_shield", null);

            if (existingShield != null)
                DestroyImmediate(existingShield);

            Transform oldVisual = root.transform.Find("ShieldVisual");
            if (oldVisual != null)
                DestroyImmediate(oldVisual.gameObject);

            return;
        }

        SpaceshipController ship = root.GetComponent<SpaceshipController>();

        if (ship != null)
        {
            ShipBoost boost = GetOrAdd<ShipBoost>(root);
            EditorAssetUtility.SetObject(boost, "_ship", ship);
            EditorAssetUtility.SetFloat(boost, "_rampInTime", _boostRampIn);
            EditorAssetUtility.SetFloat(boost, "_rampOutTime", _boostRampOut);
        }
        else
        {
            Debug.LogWarning("Ship Creator: o jogador nao tem SpaceshipController, por isso so adicionei o escudo (o boost precisa dele).");
        }

        PlayerShield shield = GetOrAdd<PlayerShield>(root);
        EditorAssetUtility.SetObject(health, "_shield", shield);
        EditorAssetUtility.SetObject(shield, "_visual", BuildShieldVisual(root));
    }

    // Esfera translucida que envolve a nave. So e criada/atualizada com um material atribuido;
    // sem material mantem o que ja existir (se existir). O PlayerShield liga/desliga-a em jogo.
    private GameObject BuildShieldVisual(GameObject root)
    {
        Transform existing = root.transform.Find("ShieldVisual");

        if (_shieldVisualMaterial == null)
            return existing != null ? existing.gameObject : null;

        // Mede a nave sem contar com a propria esfera (senao crescia a cada atualizacao).
        if (existing != null)
            existing.gameObject.SetActive(false);

        bool hasBounds = ShipPrefabUtility.TryGetLocalBounds(root, out Bounds bounds);

        if (existing != null)
            existing.gameObject.SetActive(true);

        GameObject visual;

        if (existing != null)
        {
            visual = existing.gameObject;
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "ShieldVisual";
            DestroyImmediate(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
        }

        float diameter = hasBounds ? bounds.extents.magnitude * 2f : 4f;
        visual.transform.localPosition = hasBounds ? bounds.center : Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * diameter;

        MeshRenderer renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _shieldVisualMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return visual;
    }

    private void FindInputActions(bool overwrite)
    {
        if (overwrite || _moveAction == null)
        {
            InputActionReference move = FindActionReference("Player", "Move");
            if (move != null) _moveAction = move;
        }

        if (overwrite || _lookAction == null)
        {
            InputActionReference look = FindActionReference("Player", "Look");
            if (look != null) _lookAction = look;
        }

        if (overwrite || _fireAction == null)
        {
            InputActionReference fire = FindActionReference("Player", "Attack");
            if (fire != null) _fireAction = fire;
        }

        if (overwrite || _reloadAction == null)
        {
            InputActionReference reload = FindActionReference("Player", "Reload");
            if (reload != null) _reloadAction = reload;
        }
    }

    // Procura nos InputActionAssets do projeto a referencia da acao <mapa>/<acao>.
    private static InputActionReference FindActionReference(string mapName, string actionName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:InputActionAsset"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is not InputActionReference reference)
                    continue;

                InputAction action = reference.action;
                if (action != null && action.actionMap != null &&
                    action.actionMap.name == mapName && action.name == actionName)
                    return reference;
            }
        }

        return null;
    }

    private void AddExtraScripts(GameObject root)
    {
        foreach (MonoScript script in _playerExtraScripts)
        {
            if (script == null)
                continue;

            System.Type type = script.GetClass();

            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Ship Creator: '{script.name}' nao e um componente valido.");
                continue;
            }

            if (root.GetComponent(type) == null)
                root.AddComponent(type);
        }
    }

    // =============================================================== Satelite

    private void DrawSatelliteTab()
    {
        EditorGUILayout.LabelField("Prefab do satelite", EditorStyles.boldLabel);
        Prop("_satelliteName", "Nome");
        Prop("_satelliteFolder", "Pasta");
        Prop("_satelliteModel", "Modelo do aro");
        Prop("_satelliteModelEuler", "Rotacao do modelo (graus)");

        EditorGUILayout.HelpBox(
            "O aro tem de ficar com o buraco virado para o eixo Z (azul) da raiz do prefab: o jogador atravessa-o ao longo de Z. " +
            "Se o modelo vier deitado (comum em torus exportados), ajusta a rotacao do modelo (por exemplo X = 90) " +
            "e confirma com o gizmo cian do SatelliteRing (seleciona o satelite na cena).",
            MessageType.Info);

        Prop("_satelliteMeshCollider", "Collider no aro (MeshCollider so na estrutura)");
        _satelliteLayer = EditorGUILayout.LayerField("Layer", _satelliteLayer);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Parametros do aro", EditorStyles.boldLabel);
        Prop("_satellite", "Aro, boost e escudo");

        EditorGUILayout.Space();
        Prop("_replaceModel", "Substituir modelo (se o prefab ja existir)");

        string error = ValidateSatellitePrefab();
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else if (_satelliteLayer == 0)
            EditorGUILayout.HelpBox("O satelite vai ficar na layer Default. Usa 'Preparar layers' e escolhe Obstacle.", MessageType.Info);

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Criar / atualizar prefab do satelite", GUILayout.Height(30)))
            {
                BuildSatellitePrefab();
                GUIUtility.ExitGUI();
            }
        }
    }

    private string ValidateSatellitePrefab()
    {
        if (string.IsNullOrWhiteSpace(_satelliteName)) return "Da um nome ao satelite.";
        if (string.IsNullOrWhiteSpace(_satelliteFolder) || !_satelliteFolder.StartsWith("Assets"))
            return "A pasta tem de comecar em 'Assets'.";

        string path = $"{_satelliteFolder.TrimEnd('/')}/{_satelliteName}.prefab";
        if (_satelliteModel == null && AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            return "Falta o modelo do aro.";

        return null;
    }

    private void BuildSatellitePrefab()
    {
        _so.ApplyModifiedProperties();

        GameObject root = LoadOrCreatePrefabContents(_satelliteName, _satelliteModel, _satelliteFolder,
            out string path, out bool created);

        try
        {
            if (!created && _replaceModel)
                ReplaceModel(root, _satelliteModel);

            // A rotacao do modelo e a deste separador (como os restantes valores, e reescrita em cada atualizacao).
            Transform model = root.transform.Find("Model");
            if (model != null)
                model.localRotation = Quaternion.Euler(_satelliteModelEuler);

            if (_satelliteMeshCollider)
                AddMeshColliders(root);

            SatelliteRing ring = GetOrAdd<SatelliteRing>(root);

            // Permite focar o satelite com o botao de lock.
            GetOrAdd<AimLockTarget>(root);

            float radius = _satellite.InnerRadius;
            if (_satellite.AutoRadius && ShipPrefabUtility.TryGetLocalBounds(root, out Bounds bounds))
                radius = Mathf.Min(bounds.extents.x, bounds.extents.y) * _satellite.AutoRadiusFactor;

            EditorAssetUtility.SetFloat(ring, "_innerRadius", Mathf.Max(0.1f, radius));
            EditorAssetUtility.SetBool(ring, "_oneShot", _satellite.OneShot);
            EditorAssetUtility.SetFloat(ring, "_cooldown", _satellite.Cooldown);
            EditorAssetUtility.SetFloat(ring, "_speedMultiplier", _satellite.SpeedMultiplier);
            EditorAssetUtility.SetFloat(ring, "_boostDuration", _satellite.BoostDuration);
            EditorAssetUtility.SetFloat(ring, "_shieldAmount", _satellite.ShieldAmount);
            EditorAssetUtility.SetFloat(ring, "_shieldDuration", _satellite.ShieldDuration);

            if (_satellite.PassClip != null)
                EditorAssetUtility.SetObject(ring, "_passClip", _satellite.PassClip);

            ShipPrefabUtility.SetLayerRecursively(root, _satelliteLayer);

            RemoveMissingScripts(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            _satellitePrefabToPlace = prefab;

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"Ship Creator: prefab do satelite {(created ? "criado" : "atualizado")} em {path} " +
                      $"(raio interior {radius:0.#}).", prefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        _so.Update();
    }

    // MeshCollider (nao convexo) em cada malha do aro, para o jogador bater na estrutura mas poder passar no buraco.
    // Nao faz nada se o prefab ja tiver algum collider.
    private static void AddMeshColliders(GameObject root)
    {
        if (root.GetComponentInChildren<Collider>() != null)
        {
            Debug.Log($"Ship Creator: '{root.name}' ja tem collider, nao adicionei outro.");
            return;
        }

        int added = 0;

        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
                continue;

            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            added++;
        }

        if (added == 0)
            Debug.LogWarning($"Ship Creator: '{root.name}' nao tem malhas, collider nao adicionado.");
    }

    // =============================================================== Cena

    private void DrawSceneTab()
    {
        // ---- Pools
        EditorGUILayout.LabelField("Pools de projeteis", EditorStyles.boldLabel);
        Prop("_poolInitialCapacity", "Capacidade inicial");
        Prop("_poolMaxCapacity", "Capacidade maxima");
        Prop("_applyHitMask", "Escrever o hitMask no prefab do projetil");

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Jogador", EditorStyles.miniBoldLabel);
        Prop("_playerProjectilePrefab", "Prefab do projetil");
        Prop("_playerHitMask", "Hit mask");
        Prop("_playerPool", "Pool");

        if (GUILayout.Button("Criar pool do jogador"))
        {
            _playerPool = CreatePool("PlayerProjectilePool", _playerProjectilePrefab, _playerHitMask);
            _so.Update();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Inimigos", EditorStyles.miniBoldLabel);
        Prop("_enemyProjectilePrefab", "Prefab do projetil");
        Prop("_enemyHitMask", "Hit mask");
        Prop("_enemyPool", "Pool");

        if (GUILayout.Button("Criar pool dos inimigos"))
        {
            _enemyPool = CreatePool("EnemyProjectilePool", _enemyProjectilePrefab, _enemyHitMask);
            _so.Update();
            GUIUtility.ExitGUI();
        }

        // ---- Jogador
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Jogador", EditorStyles.boldLabel);
        Prop("_playerPrefabToPlace", "Prefab do jogador");

        string playerError = ValidatePlace();
        using (new EditorGUI.DisabledScope(playerError != null || _playerPrefabToPlace == null))
        {
            if (GUILayout.Button("Colocar jogador na cena", GUILayout.Height(26)))
            {
                PlacePlayer();
                GUIUtility.ExitGUI();
            }
        }

        using (new EditorGUI.DisabledScope(playerError != null))
        {
            if (GUILayout.Button("Configurar camara para o jogador da cena"))
            {
                SetupCameraForScenePlayer();
                GUIUtility.ExitGUI();
            }
        }

        if (playerError != null)
            EditorGUILayout.HelpBox(playerError, MessageType.Warning);

        // ---- Satelites
        DrawSatelliteSpawn();

        // ---- Inimigos
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inimigos", EditorStyles.boldLabel);
        Prop("_mode", "Modo");

        for (int i = 0; i < _entries.Count; i++)
        {
            SpawnEntry entry = _entries[i];

            using (new EditorGUILayout.HorizontalScope())
            {
                entry.Prefab = (GameObject)EditorGUILayout.ObjectField(entry.Prefab, typeof(GameObject), false);
                entry.Count = Mathf.Max(1, EditorGUILayout.IntField(entry.Count, GUILayout.Width(50)));

                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }
        }

        if (GUILayout.Button("+ Adicionar tipo"))
            _entries.Add(new SpawnEntry());

        Prop("_target", "Alvo (vazio = jogador na cena)");
        _spreadRadius = Mathf.Max(0f, EditorGUILayout.FloatField("Raio de dispersao", _spreadRadius));

        if (_mode == SpawnMode.Flock)
            Prop("_existingFlock", "Bando existente (opcional)");

        string error = ValidateSpawn();
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else if (ResolveTarget() == null)
            EditorGUILayout.HelpBox("Sem alvo e sem jogador na cena, os inimigos nao vao perseguir nem disparar a nada.", MessageType.Info);
        else if (_enemyPool == null && AnyEntryHasGun())
            EditorGUILayout.HelpBox("Ha inimigos com arma mas sem pool de projeteis: nao vao conseguir disparar.", MessageType.Info);

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Colocar inimigos na cena", GUILayout.Height(30)))
            {
                Spawn();
                GUIUtility.ExitGUI();
            }
        }
    }

    private string ValidatePlace()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "Colocar objetos so funciona em modo de edicao.";

        if (_playerPrefabToPlace != null && !EditorUtility.IsPersistent(_playerPrefabToPlace))
            return "O prefab do jogador tem de ser um asset, nao um objeto da cena.";

        return null;
    }

    private ProjectilePool CreatePool(string objectName, GameObject projectilePrefab, LayerMask hitMask)
    {
        _so.ApplyModifiedProperties();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Ship Creator: cria as pools em modo de edicao.");
            return null;
        }

        if (projectilePrefab == null || !EditorUtility.IsPersistent(projectilePrefab) ||
            projectilePrefab.GetComponent<Projectile>() == null)
        {
            Debug.LogWarning("Ship Creator: indica um prefab (asset) com o componente Projectile.");
            return null;
        }

        string prefabPath = AssetDatabase.GetAssetPath(projectilePrefab);

        if (_applyHitMask)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                Projectile projectile = contents.GetComponent<Projectile>();
                EditorAssetUtility.SetInt(projectile, "hitMask", hitMask.value);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        Projectile prefabProjectile = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<Projectile>();

        GameObject poolObject = new GameObject(objectName);
        Undo.RegisterCreatedObjectUndo(poolObject, "Create projectile pool");

        ProjectilePool pool = poolObject.AddComponent<ProjectilePool>();
        EditorAssetUtility.SetObject(pool, "projectilePrefab", prefabProjectile);
        EditorAssetUtility.SetInt(pool, "initialCapacity", _poolInitialCapacity);
        EditorAssetUtility.SetInt(pool, "maxCapacity", Mathf.Max(_poolInitialCapacity, _poolMaxCapacity));

        Selection.activeGameObject = poolObject;
        Debug.Log($"Ship Creator: pool '{objectName}' criada.", poolObject);
        return pool;
    }

    private void PlacePlayer()
    {
        _so.ApplyModifiedProperties();

        if (FindFirstObjectByType<PlayerHealth>() != null &&
            !EditorUtility.DisplayDialog("Ship Creator", "Ja existe um jogador na cena. Adicionar outro?", "Adicionar", "Cancelar"))
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_playerPrefabToPlace);
        Undo.RegisterCreatedObjectUndo(instance, "Place player");
        instance.transform.position = GetSpawnCenter();

        // Cada arma tem o seu GunController (na raiz ou nos filhos): todos precisam da pool para disparar.
        GunController[] playerGuns = instance.GetComponentsInChildren<GunController>(true);

        foreach (GunController playerGun in playerGuns)
        {
            if (_playerPool != null)
                EditorAssetUtility.SetObject(playerGun, "projectilePool", _playerPool);
        }

        if (playerGuns.Length > 0 && _playerPool == null)
            Debug.LogWarning("Ship Creator: o jogador tem arma mas nao ha pool do jogador ligada.");

        if (_setupCameraOnPlace)
            SetupCamera(instance.transform, ReadInvertForward(instance));

        Selection.activeGameObject = instance;
        Debug.Log("Ship Creator: jogador colocado na cena.", instance);
    }

    private void SetupCameraForScenePlayer()
    {
        _so.ApplyModifiedProperties();

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogWarning("Ship Creator: nao ha nenhum jogador (PlayerHealth) na cena.");
            return;
        }

        SetupCamera(player.transform, ReadInvertForward(player.gameObject));
    }

    // Usa o invertForward da propria nave (se tiver SpaceshipController), para a camara nunca ficar desalinhada.
    private bool ReadInvertForward(GameObject player)
    {
        SpaceshipController ship = player.GetComponent<SpaceshipController>();
        if (ship == null)
            return _movement.InvertForward;

        SerializedProperty property = new SerializedObject(ship).FindProperty("invertForward");
        return property != null ? property.boolValue : _movement.InvertForward;
    }

    // Reaproveita a camara da cena (ou cria uma). A camara nunca e filha da nave: o CameraFollow suaviza o seguimento.
    private void SetupCamera(Transform target, bool invertForward)
    {
        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        GameObject cameraObject;

        if (camera != null)
        {
            cameraObject = camera.gameObject;
        }
        else
        {
            cameraObject = new GameObject("Player Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create camera");
            cameraObject.AddComponent<Camera>();

            if (FindFirstObjectByType<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
        }

        CameraFollow follow = cameraObject.GetComponent<CameraFollow>();
        if (follow == null)
            follow = Undo.AddComponent<CameraFollow>(cameraObject);

        EditorAssetUtility.SetObject(follow, "target", target);
        EditorAssetUtility.SetBool(follow, "invertForward", invertForward);
        EditorAssetUtility.SetFloat(follow, "distance", _cameraSettings.Distance);
        EditorAssetUtility.SetFloat(follow, "height", _cameraSettings.Height);
        EditorAssetUtility.SetFloat(follow, "lookAheadDistance", _cameraSettings.LookAheadDistance);
        EditorAssetUtility.SetFloat(follow, "positionSmoothTime", _cameraSettings.PositionSmoothTime);
        EditorAssetUtility.SetFloat(follow, "rotationSharpness", _cameraSettings.RotationSharpness);

        SetupBoostEffect(cameraObject, follow);

        // Posiciona a camara ja em modo de edicao, como o CameraFollow faria no arranque.
        Vector3 forward = target.forward * (invertForward ? -1f : 1f);
        Vector3 position = target.position - forward * _cameraSettings.Distance + target.up * _cameraSettings.Height;
        Vector3 lookPoint = target.position + forward * _cameraSettings.LookAheadDistance;

        Undo.RecordObject(cameraObject.transform, "Setup camera");
        cameraObject.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(lookPoint - position, target.up));

        Debug.Log($"Ship Creator: camara '{cameraObject.name}' a seguir '{target.name}'.", cameraObject);
    }

    // Efeito FTL da camara. Se a opcao estiver desligada nao mexe no que ja la estiver.
    private void SetupBoostEffect(GameObject cameraObject, CameraFollow follow)
    {
        if (!_cameraBoostEffect)
            return;

        CameraBoostEffect effect = cameraObject.GetComponent<CameraBoostEffect>();
        if (effect == null)
            effect = Undo.AddComponent<CameraBoostEffect>(cameraObject);

        EditorAssetUtility.SetObject(effect, "_follow", follow);
        EditorAssetUtility.SetFloat(effect, "_fovIncrease", _boostFovIncrease);
        EditorAssetUtility.SetFloat(effect, "_punchFov", _boostPunchFov);
        EditorAssetUtility.SetFloat(effect, "_extraDistance", _boostPullBack);
    }

    // ------------------------------------------------------------ Satelites na cena

    private void DrawSatelliteSpawn()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Satelites", EditorStyles.boldLabel);
        Prop("_satellitePrefabToPlace", "Prefab do satelite");
        Prop("_satelliteCount", "Quantidade");
        Prop("_satelliteSpread", "Raio de dispersao");
        Prop("_satelliteMinSpacing", "Distancia minima entre satelites");
        Prop("_satelliteMinScale", "Escala minima");
        Prop("_satelliteMaxScale", "Escala maxima");
        Prop("_satelliteFacePlayer", "Virar o aro para o jogador");

        string error = ValidateSatelliteSpawn();
        if (error != null)
            EditorGUILayout.HelpBox(error, MessageType.Warning);
        else if (_satelliteFacePlayer && FindFirstObjectByType<PlayerHealth>() == null)
            EditorGUILayout.HelpBox("Sem jogador na cena, os aros ficam com rotacao aleatoria.", MessageType.Info);

        using (new EditorGUI.DisabledScope(error != null))
        {
            if (GUILayout.Button("Colocar satelites na cena", GUILayout.Height(28)))
            {
                SpawnSatellites();
                GUIUtility.ExitGUI();
            }
        }
    }

    private string ValidateSatelliteSpawn()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "Colocar satelites so funciona em modo de edicao.";

        if (_satellitePrefabToPlace == null)
            return "Indica o prefab do satelite (cria-o no separador Satelite).";

        if (!EditorUtility.IsPersistent(_satellitePrefabToPlace))
            return "O prefab do satelite tem de ser um asset, nao um objeto da cena.";

        if (_satellitePrefabToPlace.GetComponent<SatelliteRing>() == null)
            return $"'{_satellitePrefabToPlace.name}' nao tem SatelliteRing.";

        return null;
    }

    private void SpawnSatellites()
    {
        _so.ApplyModifiedProperties();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        Transform player = playerHealth != null ? playerHealth.transform : null;

        GameObject group = new GameObject("Satellites");
        Undo.RegisterCreatedObjectUndo(group, "Spawn satellites");

        Vector3 center = GetSpawnCenter();
        List<Vector3> placed = new List<Vector3>();
        int crowded = 0;

        float minScale = Mathf.Min(_satelliteMinScale, _satelliteMaxScale);
        float maxScale = Mathf.Max(_satelliteMinScale, _satelliteMaxScale);

        for (int i = 0; i < _satelliteCount; i++)
        {
            Vector3 position = center;
            bool found = false;

            for (int attempt = 0; attempt < 30 && !found; attempt++)
            {
                position = center + Random.insideUnitSphere * _satelliteSpread;
                found = IsFarEnough(position, placed, _satelliteMinSpacing);
            }

            if (!found)
                crowded++;

            placed.Add(position);

            Quaternion rotation = _satelliteFacePlayer && player != null
                ? GetFacing(position, player) * Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward)
                : Random.rotation;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(_satellitePrefabToPlace);
            Undo.RegisterCreatedObjectUndo(instance, "Spawn satellites");

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(group.transform, true);
            instance.transform.localScale = Vector3.one * Random.Range(minScale, maxScale);
        }

        Selection.activeGameObject = group;
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Ship Creator: {_satelliteCount} satelite(s) colocados.", group);

        if (crowded > 0)
            Debug.LogWarning($"Ship Creator: {crowded} satelite(s) ficaram mais perto que a distancia minima " +
                             "(aumenta o raio de dispersao ou diminui a distancia minima).", group);
    }

    private static bool IsFarEnough(Vector3 position, List<Vector3> placed, float minDistance)
    {
        float sqr = minDistance * minDistance;

        foreach (Vector3 other in placed)
        {
            if ((other - position).sqrMagnitude < sqr)
                return false;
        }

        return true;
    }

    // ------------------------------------------------------------ Inimigos na cena

    private string ValidateSpawn()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return "Colocar inimigos so funciona em modo de edicao.";

        if (_entries.Count == 0)
            return "Adiciona pelo menos um tipo de inimigo.";

        foreach (SpawnEntry entry in _entries)
        {
            if (entry.Prefab == null)
                return "Ha uma linha sem prefab.";

            if (!EditorUtility.IsPersistent(entry.Prefab))
                return $"'{entry.Prefab.name}' tem de ser um prefab (asset), nao um objeto da cena.";

            if (entry.Prefab.GetComponent<SteeringAgent>() == null)
                return $"'{entry.Prefab.name}' nao tem SteeringAgent.";
        }

        return null;
    }

    private bool AnyEntryHasGun()
    {
        foreach (SpawnEntry entry in _entries)
        {
            if (entry.Prefab != null && entry.Prefab.GetComponent<GunController>() != null)
                return true;
        }

        return false;
    }

    private Transform ResolveTarget()
    {
        if (_target != null)
            return _target;

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        return player != null ? player.transform : null;
    }

    private void Spawn()
    {
        _so.ApplyModifiedProperties();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Transform target = ResolveTarget();
        Flock flock = null;
        GameObject group;

        if (_mode == SpawnMode.Flock)
        {
            flock = _existingFlock != null ? _existingFlock : CreateFlock(target);
            group = flock.gameObject;
        }
        else
        {
            group = new GameObject("Enemies");
            Undo.RegisterCreatedObjectUndo(group, "Spawn enemies");
        }

        Vector3 center = GetSpawnCenter();
        int total = 0;

        foreach (SpawnEntry entry in _entries)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                Vector3 position = center + Random.insideUnitSphere * _spreadRadius;

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Spawn enemies");

                instance.transform.SetPositionAndRotation(position, GetFacing(position, target));
                instance.transform.SetParent(group.transform, true);

                ConfigureInstance(instance, flock, target);
                total++;
            }
        }

        Selection.activeGameObject = group;
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"Ship Creator: {total} inimigo(s) colocados ({_mode}).", group);
    }

    private Flock CreateFlock(Transform target)
    {
        GameObject flockObject = new GameObject("Flock");
        Undo.RegisterCreatedObjectUndo(flockObject, "Create flock");
        flockObject.transform.position = GetSpawnCenter();

        Flock flock = flockObject.AddComponent<Flock>();
        EditorAssetUtility.SetObject(flock, "_target", target);

        return flock;
    }

    private void ConfigureInstance(GameObject instance, Flock flock, Transform target)
    {
        SteeringAgent agent = instance.GetComponent<SteeringAgent>();

        if (flock != null)
            EditorAssetUtility.SetObject(agent, "_flock", flock);
        else
            EditorAssetUtility.SetObject(agent, "_target", target);

        GunController gun = instance.GetComponent<GunController>();
        if (gun != null)
            EditorAssetUtility.SetObject(gun, "projectilePool", _enemyPool);

        // Se ja houver pontuacao na cena, o inimigo passa a dar pontos ao morrer.
        EnemyHealth health = instance.GetComponent<EnemyHealth>();
        ScoreManager score = FindFirstObjectByType<ScoreManager>();
        if (health != null && score != null)
            EditorAssetUtility.SetObject(health, "scoreManager", score);
    }

    private static Quaternion GetFacing(Vector3 position, Transform target)
    {
        if (target == null)
            return Quaternion.identity;

        Vector3 toTarget = target.position - position;
        return toTarget.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;
    }

    // Os objetos nascem em volta do centro da vista da Scene.
    private static Vector3 GetSpawnCenter()
    {
        SceneView view = SceneView.lastActiveSceneView;
        return view != null ? view.pivot : Vector3.zero;
    }

    // =============================================================== Sistemas (mira, lock de alvo, radar, pontuacao)

    private void DrawSystemsTab()
    {
        EditorGUILayout.HelpBox(
            "Liga na cena os sistemas que nao fazem parte dos prefabs: mira, lock de alvo com aim assist, radar e pontuacao. " +
            "Mira e radar precisam do jogador na cena (separador Cena). Os botoes sao seguros de repetir: reaproveitam o que ja existir.",
            MessageType.Info);

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.HelpBox("Estes sistemas so podem ser criados em modo de edicao.", MessageType.Warning);
            return;
        }

        bool hasPlayer = FindFirstObjectByType<PlayerHealth>() != null;
        if (!hasPlayer)
            EditorGUILayout.HelpBox("Sem jogador (PlayerHealth) na cena: a mira, o lock e o radar ficam desativados.", MessageType.Warning);

        Prop("_hudFolder", "Pasta dos assets de UI");

        // ---- Mira + lock de alvo
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mira e lock de alvo", EditorStyles.boldLabel);
        Prop("_crosshairSprite", "Sprite da mira (vazio = circulo)");
        Prop("_crosshairSize", "Tamanho da mira");
        Prop("_aimDistance", "Distancia da mira");
        Prop("_lockSelectionRadius", "Raio de selecao (fracao do ecra)");
        Prop("_lockCameraFocusBlend", "Foco da camara no alvo");
        Prop("_aimAssistLevel", "Aim assist (0 = desligado)");

        EditorGUILayout.HelpBox(
            "Cria o crosshair (AimPositioner) e o TargetLockSystem com AimAssist. Em jogo, o botao do meio do rato " +
            "seleciona o inimigo mais perto da mira (precisa de RadarTarget); a camara olha para ele e a mira e puxada para o ponto de lead.",
            MessageType.None);

        using (new EditorGUI.DisabledScope(!hasPlayer))
        {
            if (GUILayout.Button("Configurar mira + lock de alvo", GUILayout.Height(26)))
            {
                SetupAimAndLock();
                GUIUtility.ExitGUI();
            }
        }

        // ---- Radar
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Radar", EditorStyles.boldLabel);
        Prop("_radarRange", "Alcance (m)");
        Prop("_radarSize", "Tamanho no ecra (px)");

        using (new EditorGUI.DisabledScope(!hasPlayer))
        {
            if (GUILayout.Button("Configurar radar", GUILayout.Height(26)))
            {
                SetupRadar();
                GUIUtility.ExitGUI();
            }
        }

        // ---- Pontuacao
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pontuacao", EditorStyles.boldLabel);

        if (GUILayout.Button("Configurar pontuacao (ScoreManager + texto)", GUILayout.Height(26)))
        {
            SetupScore();
            GUIUtility.ExitGUI();
        }

        // ---- Inimigos ja na cena
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inimigos ja na cena", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Acrescenta RadarTarget e liga o ScoreManager a todos os inimigos (SteeringAgent) que ja estao na cena.",
            MessageType.None);

        if (GUILayout.Button("Ligar inimigos da cena aos sistemas"))
        {
            LinkSceneEnemies();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!hasPlayer))
        {
            if (GUILayout.Button("Fazer tudo (mira + lock + radar + pontuacao)", GUILayout.Height(32)))
            {
                SetupAimAndLock();
                SetupRadar();
                SetupScore();
                GUIUtility.ExitGUI();
            }
        }
    }

    private void SetupAimAndLock()
    {
        _so.ApplyModifiedProperties();

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogWarning("Ship Creator: nao ha nenhum jogador (PlayerHealth) na cena.");
            return;
        }

        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("Ship Creator: nao ha camara na cena (coloca o jogador com a camara configurada).");
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();

        RectTransform crosshair = GetOrCreateUiImage(canvas.transform, "Crosshair",
            _crosshairSprite != null ? _crosshairSprite : BuiltinCircle(),
            new Vector2(0.5f, 0.5f), Vector2.zero, _crosshairSize);
        crosshair.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.85f);

        // A mira projeta-se a partir do ponto de disparo (e para onde as balas vao). Sem ele, usa a nave,
        // com distancia negativa se a nave voar para -Z (invertForward).
        Transform firingPoint = player.transform.Find("FiringPoint");
        float distance = firingPoint != null || !ReadInvertForward(player.gameObject) ? _aimDistance : -_aimDistance;

        AimPositioner positioner = canvas.GetComponent<AimPositioner>();
        if (positioner == null)
            positioner = Undo.AddComponent<AimPositioner>(canvas.gameObject);

        EditorAssetUtility.SetObject(positioner, "shipTransform", player.transform);
        EditorAssetUtility.SetObject(positioner, "aimOrigin", firingPoint);
        EditorAssetUtility.SetObject(positioner, "aimRect", crosshair);
        EditorAssetUtility.SetObject(positioner, "aimCamera", camera);
        EditorAssetUtility.SetFloat(positioner, "aimDistance", distance);

        // O TargetLockSystem constroi a propria UI e cria o AimAssist se faltar, mas em edicao
        // o Awake nao corre, por isso o AimAssist e adicionado aqui.
        TargetLockSystem lockSystem = FindFirstObjectByType<TargetLockSystem>();
        if (lockSystem == null)
        {
            GameObject lockObject = new GameObject("TargetLockSystem");
            Undo.RegisterCreatedObjectUndo(lockObject, "Create target lock");
            lockSystem = Undo.AddComponent<TargetLockSystem>(lockObject);
        }

        EditorAssetUtility.SetFloat(lockSystem, "selectionRadius", _lockSelectionRadius);
        EditorAssetUtility.SetFloat(lockSystem, "cameraFocusBlend", _lockCameraFocusBlend);
        EditorAssetUtility.SetFloat(lockSystem, "aimDistance", Mathf.Abs(_aimDistance));

        AimAssist assist = lockSystem.GetComponent<AimAssist>();
        if (assist == null)
            assist = Undo.AddComponent<AimAssist>(lockSystem.gameObject);

        EditorAssetUtility.SetFloat(assist, "assistLevel", _aimAssistLevel);

        LinkSceneEnemies();

        Selection.activeGameObject = lockSystem.gameObject;
        Debug.Log("Ship Creator: mira (AimPositioner) e lock de alvo (TargetLockSystem + AimAssist) configurados.", lockSystem);
    }

    private void SetupRadar()
    {
        _so.ApplyModifiedProperties();

        PlayerHealth player = FindFirstObjectByType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogWarning("Ship Creator: nao ha nenhum jogador (PlayerHealth) na cena.");
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();

        RectTransform container = GetOrCreateUiImage(canvas.transform, "Radar", BuiltinCircle(),
            new Vector2(1f, 0f), new Vector2(-24f, 24f), Vector2.one * _radarSize);
        container.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

        RadarUI radar = container.GetComponent<RadarUI>();
        if (radar == null)
            radar = Undo.AddComponent<RadarUI>(container.gameObject);

        EditorAssetUtility.SetObject(radar, "_ship", player.transform);
        EditorAssetUtility.SetBool(radar, "_invertForward", ReadInvertForward(player.gameObject));
        EditorAssetUtility.SetObject(radar, "_container", container);
        EditorAssetUtility.SetObject(radar, "_blipPrefab", GetOrCreateBlipPrefab());
        EditorAssetUtility.SetFloat(radar, "_range", _radarRange);

        LinkSceneEnemies();

        Debug.Log("Ship Creator: radar configurado (canto inferior direito do HUD).", radar);
    }

    private void SetupScore()
    {
        _so.ApplyModifiedProperties();

        ScoreManager manager = FindFirstObjectByType<ScoreManager>();
        if (manager == null)
        {
            GameObject managerObject = new GameObject("ScoreManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create score manager");
            manager = Undo.AddComponent<ScoreManager>(managerObject);
        }

        Canvas canvas = GetOrCreateHudCanvas();
        Transform existing = canvas.transform.Find("ScoreText");
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;

        if (text == null)
        {
            GameObject textObject = existing != null ? existing.gameObject : new GameObject("ScoreText", typeof(RectTransform));

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(textObject, "Create score text");
                textObject.transform.SetParent(canvas.transform, false);
            }

            text = Undo.AddComponent<TextMeshProUGUI>(textObject);
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -24f);
        rect.sizeDelta = new Vector2(400f, 50f);

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32f;
        text.raycastTarget = false;
        text.text = "Score: 0";

        ScoreUI ui = text.GetComponent<ScoreUI>();
        if (ui == null)
            ui = Undo.AddComponent<ScoreUI>(text.gameObject);

        EditorAssetUtility.SetObject(ui, "scoreText", text);
        EditorAssetUtility.SetObject(ui, "scoreManager", manager);

        LinkSceneEnemies();

        Debug.Log("Ship Creator: pontuacao configurada (ScoreManager + texto no topo do HUD).", manager);
    }

    // Poe RadarTarget e liga o ScoreManager nos inimigos que ja estao na cena.
    private void LinkSceneEnemies()
    {
        ScoreManager score = FindFirstObjectByType<ScoreManager>();
        int radarAdded = 0;
        int scoreLinked = 0;

        foreach (SteeringAgent agent in FindObjectsByType<SteeringAgent>(FindObjectsSortMode.None))
        {
            if (agent.GetComponent<RadarTarget>() == null)
            {
                Undo.AddComponent<RadarTarget>(agent.gameObject);
                radarAdded++;
            }

            EnemyHealth health = agent.GetComponent<EnemyHealth>();
            if (score != null && health != null)
            {
                EditorAssetUtility.SetObject(health, "scoreManager", score);
                scoreLinked++;
            }
        }

        Debug.Log($"Ship Creator: {radarAdded} inimigo(s) com RadarTarget novo, {scoreLinked} ligado(s) ao ScoreManager.");
    }

    private static Canvas GetOrCreateHudCanvas()
    {
        GameObject existing = GameObject.Find("HUD Canvas");
        if (existing != null && existing.TryGetComponent(out Canvas found))
            return found;

        GameObject canvasObject = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create HUD canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // Imagem de UI filha de 'parent'. Se ja existir uma com este nome, e reaproveitada e reposicionada.
    private static RectTransform GetOrCreateUiImage(Transform parent, string objectName, Sprite sprite,
        Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(objectName);
        GameObject imageObject;

        if (existing != null)
        {
            imageObject = existing.gameObject;
        }
        else
        {
            imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(imageObject, $"Create {objectName}");
            imageObject.transform.SetParent(parent, false);
        }

        RectTransform rect = (RectTransform)imageObject.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        if (image == null)
            image = Undo.AddComponent<Image>(imageObject);

        image.sprite = sprite;
        image.raycastTarget = false;

        return rect;
    }

    private static Sprite BuiltinCircle() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

    // O RadarUI instancia um Image por inimigo: precisa de um prefab ativo (um objeto de cena desativado criaria blips invisiveis).
    private Image GetOrCreateBlipPrefab()
    {
        string folder = _hudFolder.TrimEnd('/');
        EditorAssetUtility.EnsureFolder(folder);
        string path = $"{folder}/RadarBlip.prefab";

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null && existing.TryGetComponent(out Image existingImage))
            return existingImage;

        GameObject temp = new GameObject("RadarBlip", typeof(RectTransform), typeof(Image));

        try
        {
            Image image = temp.GetComponent<Image>();
            image.sprite = BuiltinCircle();
            image.raycastTarget = false;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            return saved.GetComponent<Image>();
        }
        finally
        {
            DestroyImmediate(temp);
        }
    }

    // =============================================================== Layers

    private void SetupLayers()
    {
        _so.ApplyModifiedProperties();

        int player = EditorAssetUtility.EnsureLayer(PlayerLayerName);
        int enemy = EditorAssetUtility.EnsureLayer(EnemyLayerName);
        int obstacle = EditorAssetUtility.EnsureLayer(ObstacleLayerName);

        if (player < 0 || enemy < 0 || obstacle < 0)
            return;

        _playerLayer = player;
        _enemyLayer = enemy;
        _obstacleLayer = obstacle;
        _satelliteLayer = obstacle;

        // Obstaculos dos inimigos: so a layer Obstacle (se incluisses o jogador, fugiam dele).
        _settings.SetObstacleMask(1 << obstacle);

        // Quem acerta em quê.
        _playerHitMask = (1 << enemy) | (1 << obstacle);
        _enemyHitMask = (1 << player) | (1 << obstacle);

        _so.Update();
        Debug.Log("Ship Creator: layers PlayerShip, EnemyShip e Obstacle prontas. " +
                  "Falta so a Layer Collision Matrix (Project Settings > Physics).");
    }

    // =============================================================== Prefabs (comum)

    private static GameObject LoadOrCreatePrefabContents(string prefabName, GameObject model, string folder,
        out string path, out bool created)
    {
        string cleanFolder = folder.TrimEnd('/');
        EditorAssetUtility.EnsureFolder(cleanFolder);

        path = $"{cleanFolder}/{prefabName}.prefab";
        created = AssetDatabase.LoadAssetAtPath<GameObject>(path) == null;

        if (created)
            ShipPrefabUtility.CreateBasePrefab(prefabName, model, path);

        return PrefabUtility.LoadPrefabContents(path);
    }

    private static void ReplaceModel(GameObject root, GameObject model)
    {
        if (model == null)
            return;

        Transform old = root.transform.Find("Model");
        if (old != null)
            DestroyImmediate(old.gameObject);

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
        visual.name = "Model";
    }

    private void ApplyCollider(GameObject root, ShipColliderType type)
    {
        if (_rebuildCollider)
        {
            foreach (Collider existing in root.GetComponents<Collider>())
                DestroyImmediate(existing);
        }

        ShipPrefabUtility.AddCollider(root, type);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}

// Tools > Asteroid Setup
// Cria o fragmento e o recurso, põe o AsteroidPool na cena, liga os asteroides que já tens
// e acrescenta o ResourceCollector à nave. Pode ser corrida mais do que uma vez:
// reaproveita o AsteroidPool e os componentes que já existam.
// Se indicares o prefab do asteroide e o do player, a configuração vai para os prefabs
// (e as cópias na cena herdam-na). Só o _pool, que é um objeto da cena, é ligado em cada cópia.
// Tudo o que acontece (e tudo o que falha) aparece na caixa de estado da janela e na Consola.
public class AsteroidSetupWindow : EditorWindow
{
    [SerializeField] private AsteroidSetupSettings _settings = new AsteroidSetupSettings();

    private readonly List<string> _messages = new List<string>();
    private MessageType _statusType = MessageType.None;
    private SerializedObject _serialized;
    private Vector2 _scroll;

    private void OnEnable()
    {
        _serialized = new SerializedObject(this);
        _serialized.FindProperty("_settings").isExpanded = true;
    }

    private void OnGUI()
    {
        _serialized.Update();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.PropertyField(_serialized.FindProperty("_settings"), new GUIContent("Definições"), true);
        EditorGUILayout.EndScrollView();

        _serialized.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (_messages.Count > 0)
            EditorGUILayout.HelpBox(string.Join("\n", _messages), _statusType);

        if (GUILayout.Button("1. Criar fragmento, recurso e AsteroidPool"))
            Run(() => CreateAssetsAndPool());

        if (GUILayout.Button("2. Configurar asteroides (prefab / pai / filtro de nome)"))
            Run(() => ConfigureAsteroids(CollectFromScene()));

        if (GUILayout.Button("2. Configurar objetos selecionados"))
            Run(() => ConfigureAsteroids(CollectSelection()));

        if (GUILayout.Button("3. Configurar nave (ResourceCollector)"))
            Run(() => ConfigureShip());

        if (GUILayout.Button("4. Configurar planetas selecionados (entrega com E)"))
            Run(() => ConfigurePlanets());

        EditorGUILayout.Space();

        if (GUILayout.Button("Fazer tudo (1 + 2 + 3)", GUILayout.Height(28)))
            Run(DoAll);
    }

    // ---------- Estado ----------

    private void Run(System.Action action)
    {
        _messages.Clear();
        _statusType = MessageType.None;

        try
        {
            action();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            SetStatus($"Erro: {exception.Message} (detalhes na Consola)", MessageType.Error);
        }

        GUIUtility.ExitGUI();
    }

    private void SetStatus(string message, MessageType type)
    {
        _messages.Add(message);

        if (type > _statusType)
            _statusType = type;

        string text = $"Asteroid Setup: {message}";

        if (type == MessageType.Error)
            Debug.LogError(text);
        else if (type == MessageType.Warning)
            Debug.LogWarning(text);
        else
            Debug.Log(text);

        Repaint();
    }

    private void DoAll()
    {
        AsteroidPool pool = CreateAssetsAndPool();

        if (pool == null)
            return;

        ConfigureAsteroids(CollectFromScene());
        ConfigureShip();
    }

    // ---------- 1. Assets e pool ----------

    private AsteroidPool CreateAssetsAndPool()
    {
        AsteroidSetupSettings s = _settings;
        GameObject reference = GetReferenceAsteroid();
        GameObject meshSource = null;

        if (s.FragmentModel == null)
        {
            meshSource = reference;

            if (meshSource == null || meshSource.GetComponentInChildren<MeshFilter>() == null)
            {
                SetStatus("Sem modelo do fragmento, e não encontrei um asteroide com malha de onde copiar. " +
                          "Atribui o modelo do fragmento, o prefab do asteroide, ou indica os asteroides " +
                          "(objeto pai, filtro de nome ou seleção).",
                          MessageType.Warning);
                return null;
            }
        }

        int layer = reference != null ? reference.layer : 0;

        string folder = s.OutputFolder;
        EditorAssetUtility.EnsureFolder(folder);

        Material flashMaterial = GetFlashMaterial(s);
        Asteroid fragmentPrefab = CreateFragmentPrefab(s, folder, meshSource, layer, flashMaterial);
        ResourcePickup resourcePrefab = CreateResourcePrefab(s, folder);

        AsteroidPool pool = FindFirstObjectByType<AsteroidPool>();

        if (pool == null)
        {
            GameObject poolObject = new GameObject("AsteroidPool");
            Undo.RegisterCreatedObjectUndo(poolObject, "Create AsteroidPool");
            pool = poolObject.AddComponent<AsteroidPool>();
        }

        EditorAssetUtility.SetObject(pool, "_fragmentPrefab", fragmentPrefab);
        EditorAssetUtility.SetInt(pool, "_fragmentInitialCapacity", s.FragmentInitialCapacity);
        EditorAssetUtility.SetInt(pool, "_fragmentMaxCapacity", s.FragmentMaxCapacity);
        EditorAssetUtility.SetInt(pool, "_fragmentPrewarm", s.FragmentPrewarm);

        EditorAssetUtility.SetObject(pool, "_resourcePrefab", resourcePrefab);
        EditorAssetUtility.SetInt(pool, "_resourceInitialCapacity", s.ResourceInitialCapacity);
        EditorAssetUtility.SetInt(pool, "_resourceMaxCapacity", s.ResourceMaxCapacity);
        EditorAssetUtility.SetInt(pool, "_resourcePrewarm", s.ResourcePrewarm);

        EditorUtility.SetDirty(pool);
        EditorSceneManager.MarkSceneDirty(pool.gameObject.scene);

        SetStatus($"Prefabs criados em '{folder}' e AsteroidPool pronto na cena.", MessageType.Info);
        return pool;
    }

    private static Asteroid CreateFragmentPrefab(AsteroidSetupSettings s, string folder, GameObject meshSource, int layer,
        Material flashMaterial)
    {
        string path = $"{folder}/AsteroidFragment.prefab";

        if (s.FragmentModel != null)
            ShipPrefabUtility.CreateBasePrefab("AsteroidFragment", s.FragmentModel, path);
        else
            CreateFragmentBaseFromAsteroid(path, meshSource);

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            ShipPrefabUtility.SetLayerRecursively(contents, layer);

            Rigidbody body = contents.AddComponent<Rigidbody>();
            ShipPrefabUtility.ConfigureRigidbody(body);

            ShipPrefabUtility.AddCollider(contents, s.ColliderType);

            Asteroid asteroid = contents.AddComponent<Asteroid>();

            // As gerações que ainda faltam são decididas na hora por quem larga o fragmento.
            ApplyAsteroidValues(asteroid, s, s.FragmentHealth, s.FragmentLifetime, 0,
                s.FragmentResourceCount, s.FragmentDropChance);

            if (flashMaterial != null)
                ApplyHitFlash(contents.AddComponent<HitFlash>(), s, flashMaterial);

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<Asteroid>();
    }

    // Prefab base (raiz + malha) copiado do primeiro asteroide, para quando não há modelo de fragmento.
    private static void CreateFragmentBaseFromAsteroid(string path, GameObject source)
    {
        MeshFilter sourceFilter = source.GetComponentInChildren<MeshFilter>();
        Renderer sourceRenderer = sourceFilter.GetComponent<Renderer>();

        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject root = new GameObject("AsteroidFragment");
            SceneManager.MoveGameObjectToScene(root, previewScene);

            GameObject visual = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer));
            SceneManager.MoveGameObjectToScene(visual, previewScene);
            visual.transform.SetParent(root.transform, false);

            visual.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;

            if (sourceRenderer != null)
                visual.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;

            // Mantém a posição e a escala da malha em relação à raiz do asteroide.
            if (sourceFilter.transform != source.transform)
            {
                visual.transform.localPosition = sourceFilter.transform.localPosition;
                visual.transform.localRotation = sourceFilter.transform.localRotation;
                visual.transform.localScale = sourceFilter.transform.localScale;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    private static ResourcePickup CreateResourcePrefab(AsteroidSetupSettings s, string folder)
    {
        string path = $"{folder}/AsteroidResource.prefab";

        if (s.ResourceModel != null)
            ShipPrefabUtility.CreateBasePrefab("AsteroidResource", s.ResourceModel, path);
        else
            CreatePrimitiveResourcePrefab(path);

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            contents.transform.localScale = Vector3.one * s.ResourceScale;

            // O recurso é apanhado por distância, por isso não precisa de colliders.
            foreach (Collider collider in contents.GetComponentsInChildren<Collider>())
                DestroyImmediate(collider);

            if (s.ResourceMaterial != null)
            {
                foreach (Renderer meshRenderer in contents.GetComponentsInChildren<Renderer>())
                    meshRenderer.sharedMaterial = s.ResourceMaterial;
            }

            ResourcePickup pickup = contents.AddComponent<ResourcePickup>();
            EditorAssetUtility.SetFloat(pickup, "_drag", s.ResourceDrag);
            EditorAssetUtility.SetFloat(pickup, "_lifetime", s.ResourceLifetime);

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<ResourcePickup>();
    }

    // Prefab minimo (raiz + esfera) para quando não há modelo de recurso.
    private static void CreatePrimitiveResourcePrefab(string path)
    {
        Scene previewScene = EditorSceneManager.NewPreviewScene();

        try
        {
            GameObject root = new GameObject("AsteroidResource");
            SceneManager.MoveGameObjectToScene(root, previewScene);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SceneManager.MoveGameObjectToScene(visual, previewScene);
            visual.name = "Model";
            visual.transform.SetParent(root.transform, false);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }

    // ---------- 2. Asteroides ----------

    // Procura sem escrever mensagens (também é usada para descobrir o asteroide de referência).
    private List<GameObject> FindAsteroidsInScene()
    {
        List<GameObject> targets = new List<GameObject>();
        Transform parent = _settings.AsteroidsParent;
        string filter = _settings.NameFilter;

        if (string.IsNullOrEmpty(filter))
        {
            // Sem pai nem filtro: procura objetos com "Planet" no nome.
            if (parent == null)
                filter = "Planet";
        }

        if (string.IsNullOrEmpty(filter))
        {

            for (int i = 0; i < parent.childCount; i++)
                targets.Add(parent.GetChild(i).gameObject);

            return targets;
        }

        if (parent != null)
        {
            FindByName(parent, filter, targets);
            return targets;
        }

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (NameMatches(root.name, filter))
                targets.Add(root);
            else
                FindByName(root.transform, filter, targets);
        }

        return targets;
    }

    private static void FindByName(Transform node, string filter, List<GameObject> targets)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            Transform child = node.GetChild(i);

            // Não procura dentro de um asteroide já encontrado.
            if (NameMatches(child.name, filter))
                targets.Add(child.gameObject);
            else
                FindByName(child, filter, targets);
        }
    }

    private static bool NameMatches(string objectName, string filter)
    {
        return objectName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private List<GameObject> CollectFromScene()
    {
        List<GameObject> targets = FindAsteroidsInScene();

        // Com um prefab de asteroide atribuído, configurar só o prefab já é um resultado válido.
        if (targets.Count == 0 && _settings.AsteroidPrefab == null)
        {
            SetStatus("Não encontrei asteroides. Atribui o prefab do asteroide, o objeto pai (filhos diretos) " +
                      "ou um filtro de nome. Também podes selecioná-los na hierarquia e usar o botão dos objetos selecionados.",
                      MessageType.Warning);
        }

        return targets;
    }

    private List<GameObject> CollectSelection()
    {
        GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.ExcludePrefab);

        if (selected.Length == 0 && _settings.AsteroidPrefab == null)
            SetStatus("Não há objetos da cena selecionados na hierarquia.", MessageType.Warning);

        return new List<GameObject>(selected);
    }

    private GameObject GetReferenceAsteroid()
    {
        if (_settings.AsteroidPrefab != null)
            return _settings.AsteroidPrefab;

        List<GameObject> found = FindAsteroidsInScene();

        if (found.Count > 0)
            return found[0];

        return Selection.activeGameObject;
    }

    private int ConfigureAsteroids(List<GameObject> targets)
    {
        AsteroidSetupSettings s = _settings;
        Material flashMaterial = GetFlashMaterial(s);
        bool prefabDone = s.AsteroidPrefab != null && ConfigureAsteroidPrefab(s, flashMaterial);

        if (targets.Count == 0)
            return 0;

        AsteroidPool pool = FindFirstObjectByType<AsteroidPool>();

        if (pool == null)
        {
            SetStatus("Não há AsteroidPool na cena. Corre primeiro o passo 1.", MessageType.Warning);
            return 0;
        }

        if (s.ControlFromParent)
            EnsureField(s, pool);

        int configured = 0;

        foreach (GameObject target in targets)
        {
            if (target == null)
                continue;

            Asteroid asteroid = target.GetComponent<Asteroid>();
            bool needsValues = asteroid == null || !prefabDone;

            // Dentro de um AsteroidField os valores e o pool vêm dele.
            bool controlled = target.GetComponentInParent<AsteroidField>() != null;

            if (asteroid == null)
                asteroid = Undo.AddComponent<Asteroid>(target);

            if (needsValues)
            {
                // Só acrescenta collider se não houver nenhum (evita uma mensagem por asteroide).
                if (target.GetComponentInChildren<Collider>() == null)
                    ShipPrefabUtility.AddCollider(target, s.ColliderType);

                if (!controlled)
                {
                    ApplyAsteroidValues(asteroid, s, s.Health, 0f, s.FragmentGenerations,
                        s.ResourceCount, s.DropChance);
                }

                if (flashMaterial != null && target.GetComponent<HitFlash>() == null)
                    ApplyHitFlash(Undo.AddComponent<HitFlash>(target), s, flashMaterial);
            }

            // O pool é um objeto da cena, por isso só pode ser ligado nas cópias da cena.
            if (!controlled)
                EditorAssetUtility.SetObject(asteroid, "_pool", pool);

            EditorUtility.SetDirty(asteroid);
            configured++;
        }

        EditorSceneManager.MarkSceneDirty(pool.gameObject.scene);
        SetStatus($"{configured} asteroides da cena ligados ao AsteroidPool.", MessageType.Info);

        AddLayersToProjectile(s, targets);
        return configured;
    }

    private bool ConfigureAsteroidPrefab(AsteroidSetupSettings s, Material flashMaterial)
    {
        if (!TryGetPrefabPath(s.AsteroidPrefab, "Prefab do asteroide", out string path))
            return false;

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            Asteroid asteroid = contents.GetComponent<Asteroid>();

            if (asteroid == null)
                asteroid = contents.AddComponent<Asteroid>();

            if (contents.GetComponentInChildren<Collider>() == null)
                ShipPrefabUtility.AddCollider(contents, s.ColliderType);

            ApplyAsteroidValues(asteroid, s, s.Health, 0f, s.FragmentGenerations,
                s.ResourceCount, s.DropChance);

            if (flashMaterial != null)
            {
                HitFlash flash = contents.GetComponent<HitFlash>();

                if (flash == null)
                    flash = contents.AddComponent<HitFlash>();

                ApplyHitFlash(flash, s, flashMaterial);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        SetStatus($"Prefab do asteroide configurado ('{path}'). Os asteroides criados em runtime a partir dele " +
                  "precisam que o _pool seja ligado por quem os cria.", MessageType.Info);
        return true;
    }

    // Valores que os asteroides e os fragmentos têm em comum, mais os que mudam entre os dois.
    private static void ApplyAsteroidValues(Asteroid asteroid, AsteroidSetupSettings s, float health, float lifetime,
        int generations, Vector2Int resourceCount, float dropChance)
    {
        EditorAssetUtility.SetFloat(asteroid, "_health", health);
        EditorAssetUtility.SetFloat(asteroid, "_lifetime", lifetime);
        EditorAssetUtility.SetInt(asteroid, "_fragmentGenerations", generations);
        SetVector2Int(asteroid, "_fragmentCount", s.FragmentCount);
        EditorAssetUtility.SetFloat(asteroid, "_fragmentScale", s.FragmentScale);
        EditorAssetUtility.SetFloat(asteroid, "_scatterSpeed", s.ScatterSpeed);
        EditorAssetUtility.SetFloat(asteroid, "_maxSpinSpeed", s.MaxSpinSpeed);
        SetVector2Int(asteroid, "_resourceCount", resourceCount);
        EditorAssetUtility.SetFloat(asteroid, "_dropChance", dropChance);
        EditorAssetUtility.SetFloat(asteroid, "_resourceScatterSpeed", s.ResourceScatterSpeed);
        EditorAssetUtility.SetObject(asteroid, "_breakClip", s.BreakClip);
        EditorAssetUtility.SetObject(asteroid, "_breakEffect", s.BreakEffect);
    }

    // O pai dos asteroides passa a ter um AsteroidField. Se já existir, os valores dele são teus
    // e não se mexe neles: só se garante que está ligado ao pool.
    private void EnsureField(AsteroidSetupSettings s, AsteroidPool pool)
    {
        Transform parent = s.AsteroidsParent;

        if (parent == null)
        {
            SetStatus("Sem objeto pai não há onde pôr o AsteroidField: os valores foram copiados para cada asteroide.",
                MessageType.Info);
            return;
        }

        AsteroidField field = parent.GetComponent<AsteroidField>();
        bool created = field == null;

        if (created)
        {
            field = Undo.AddComponent<AsteroidField>(parent.gameObject);
            ApplyFieldValues(field, s);
        }

        EditorAssetUtility.SetObject(field, "_pool", pool);
        EditorUtility.SetDirty(field);
        EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);

        if (created)
        {
            SetStatus($"AsteroidField criado em '{parent.name}' com os valores das definições. " +
                      "Daqui em diante edita-os nele.", MessageType.Info);
        }
        else
        {
            SetStatus($"'{parent.name}' já tinha um AsteroidField: os valores dele ficaram como estavam.",
                MessageType.Info);
        }
    }

    private static void ApplyFieldValues(AsteroidField field, AsteroidSetupSettings s)
    {
        EditorAssetUtility.SetFloat(field, "_health", s.Health);
        EditorAssetUtility.SetInt(field, "_fragmentGenerations", s.FragmentGenerations);
        SetVector2Int(field, "_fragmentCount", s.FragmentCount);
        EditorAssetUtility.SetFloat(field, "_fragmentScale", s.FragmentScale);
        EditorAssetUtility.SetFloat(field, "_scatterSpeed", s.ScatterSpeed);
        EditorAssetUtility.SetFloat(field, "_maxSpinSpeed", s.MaxSpinSpeed);
        SetVector2Int(field, "_resourceCount", s.ResourceCount);
        EditorAssetUtility.SetFloat(field, "_dropChance", s.DropChance);
        EditorAssetUtility.SetFloat(field, "_resourceScatterSpeed", s.ResourceScatterSpeed);
        EditorAssetUtility.SetObject(field, "_breakClip", s.BreakClip);
        EditorAssetUtility.SetObject(field, "_breakEffect", s.BreakEffect);
    }

    // Material branco do HitFlash: o das definições, ou o HitFlash_White da pasta de saída (criado se não existir).
    private Material GetFlashMaterial(AsteroidSetupSettings s)
    {
        if (!s.HitFlash)
            return null;

        if (s.HitFlashMaterial != null)
            return s.HitFlashMaterial;

        Material material = EnemyDataFactory.LoadOrCreateFlashMaterial(s.OutputFolder);

        if (material == null)
        {
            SetStatus("Não consegui criar o material do HitFlash. Atribui um material branco nas definições.",
                MessageType.Warning);
        }

        return material;
    }

    private static void ApplyHitFlash(HitFlash flash, AsteroidSetupSettings s, Material material)
    {
        EditorAssetUtility.SetObject(flash, "_flashMaterial", material);
        EditorAssetUtility.SetFloat(flash, "_duration", s.HitFlashDuration);
    }

    // O projétil só acerta em layers que estejam no seu hitMask.
    private void AddLayersToProjectile(AsteroidSetupSettings s, List<GameObject> targets)
    {
        if (s.ProjectilePrefab == null)
        {
            SetStatus("Atribui o prefab do projétil para a layer dos asteroides entrar no hitMask do Projectile. " +
                      "O LaserBeam tem o seu próprio hitMask.", MessageType.Info);
            return;
        }

        Projectile projectile = s.ProjectilePrefab.GetComponentInChildren<Projectile>(true);

        if (projectile == null)
        {
            SetStatus("O prefab do projétil não tem o componente Projectile.", MessageType.Warning);
            return;
        }

        GameObject reference = GetReferenceAsteroid();
        int mask = reference != null ? 1 << reference.layer : 0;

        foreach (GameObject target in targets)
        {
            if (target != null)
                mask |= 1 << target.layer;
        }

        SerializedObject serialized = new SerializedObject(projectile);
        SerializedProperty hitMask = serialized.FindProperty("hitMask");

        if (hitMask == null)
        {
            SetStatus("Campo 'hitMask' não encontrado no Projectile.", MessageType.Warning);
            return;
        }

        hitMask.intValue |= mask;
        serialized.ApplyModifiedProperties();

        EditorUtility.SetDirty(projectile);
        AssetDatabase.SaveAssets();

        SetStatus("Layers dos asteroides acrescentadas ao hitMask do Projectile. O LaserBeam tem o seu próprio hitMask.",
            MessageType.Info);
    }

    // ---------- 3. Nave ----------

    private bool ConfigureShip()
    {
        AsteroidSetupSettings s = _settings;

        if (s.PlayerPrefab == null && s.Ship == null)
        {
            SetStatus("Atribui o prefab do player e/ou o objeto da nave na cena.", MessageType.Warning);
            return false;
        }

        bool prefabDone = s.PlayerPrefab != null && ConfigurePlayerPrefab(s);

        if (s.Ship != null)
        {
            ResourceCollector collector = s.Ship.GetComponent<ResourceCollector>();
            bool created = collector == null;

            if (created)
                collector = Undo.AddComponent<ResourceCollector>(s.Ship);

            // Se o prefab já foi configurado, a nave da cena herda os valores.
            if (created || !prefabDone)
                ApplyCollectorValues(collector, s);

            if (s.AttachPoint != null)
                EditorAssetUtility.SetObject(collector, "_attachPoint", s.AttachPoint);

            ResourceDelivery delivery = s.Ship.GetComponent<ResourceDelivery>();

            if (delivery == null)
                delivery = Undo.AddComponent<ResourceDelivery>(s.Ship);

            EditorAssetUtility.SetObject(delivery, "_collector", collector);
            EditorAssetUtility.SetObject(delivery, "_deliverClip", s.DeliverClip);

            EditorUtility.SetDirty(delivery);

            EditorUtility.SetDirty(collector);
            EditorSceneManager.MarkSceneDirty(s.Ship.scene);

            SetStatus($"ResourceCollector configurado na nave da cena ('{s.Ship.name}').", MessageType.Info);
            return true;
        }

        return prefabDone;
    }

    private bool ConfigurePlayerPrefab(AsteroidSetupSettings s)
    {
        if (!TryGetPrefabPath(s.PlayerPrefab, "Prefab do player", out string path))
            return false;

        GameObject contents = PrefabUtility.LoadPrefabContents(path);

        try
        {
            ResourceCollector collector = contents.GetComponent<ResourceCollector>();

            if (collector == null)
                collector = contents.AddComponent<ResourceCollector>();

            ApplyCollectorValues(collector, s);

            ResourceDelivery delivery = contents.GetComponent<ResourceDelivery>();

            if (delivery == null)
                delivery = contents.AddComponent<ResourceDelivery>();

            EditorAssetUtility.SetObject(delivery, "_deliverClip", s.DeliverClip);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        SetStatus($"Prefab do player configurado ('{path}'). O ponto de ligação (se quiseres um) define-se na nave da cena.",
            MessageType.Info);
        return true;
    }

    private static void ApplyCollectorValues(ResourceCollector collector, AsteroidSetupSettings s)
    {
        EditorAssetUtility.SetFloat(collector, "_pickupRadius", s.PickupRadius);
        EditorAssetUtility.SetInt(collector, "_maxCarried", s.MaxCarried);
        EditorAssetUtility.SetObject(collector, "_collectClip", s.CollectClip);
        EditorAssetUtility.SetBool(collector, "_invertForward", s.InvertForward);
        EditorAssetUtility.SetFloat(collector, "_spacing", s.Spacing);
        EditorAssetUtility.SetFloat(collector, "_stiffness", s.Stiffness);
        EditorAssetUtility.SetFloat(collector, "_damping", s.Damping);
        EditorAssetUtility.SetFloat(collector, "_maxStretch", s.MaxStretch);
    }

    // ---------- 4. Planetas ----------

    private bool ConfigurePlanets()
    {
        AsteroidSetupSettings s = _settings;
        GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.ExcludePrefab);

        if (selected.Length == 0)
        {
            SetStatus("Seleciona os planetas na hierarquia e carrega outra vez.", MessageType.Warning);
            return false;
        }

        foreach (GameObject planetObject in selected)
        {
            ResourcePlanet planet = planetObject.GetComponent<ResourcePlanet>();
            bool created = planet == null;

            if (created)
                planet = Undo.AddComponent<ResourcePlanet>(planetObject);

            // Aviso "[E] Entregar" e lock de alvo (botao do meio do rato) em todos os planetas.
            PlanetExtras.Ensure(planetObject);

            // Um planeta que ja tinha valores teus nao e mexido; so os novos recebem os das definicoes.
            if (created)
            {
                EditorAssetUtility.SetInt(planet, "_capacity", s.PlanetCapacity);
                EditorAssetUtility.SetFloat(planet, "_deliveryRadius", GetPlanetRadius(planetObject) + s.DeliveryMargin);
            }

            EditorUtility.SetDirty(planet);
            EditorSceneManager.MarkSceneDirty(planetObject.scene);
        }

        SetStatus($"{selected.Length} planetas prontos para receber recursos (capacidade {s.PlanetCapacity} nos novos).",
            MessageType.Info);
        return true;
    }

    private static float GetPlanetRadius(GameObject planet)
    {
        Renderer[] renderers = planet.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
    }

    // ---------- Auxiliares ----------

    private bool TryGetPrefabPath(GameObject prefab, string label, out string path)
    {
        path = AssetDatabase.GetAssetPath(prefab);
        PrefabAssetType type = PrefabUtility.GetPrefabAssetType(prefab);

        if (string.IsNullOrEmpty(path) || (type != PrefabAssetType.Regular && type != PrefabAssetType.Variant))
        {
            SetStatus($"'{label}' tem de ser um prefab guardado no Project (não um objeto da cena nem um modelo FBX).",
                MessageType.Warning);
            return false;
        }

        return true;
    }

    private static void SetVector2Int(Object target, string field, Vector2Int value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
        {
            Debug.LogWarning($"Asteroid Setup: campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }

        property.vector2IntValue = value;
        serialized.ApplyModifiedProperties();
    }
}

// Tools > Planet Setup
// Poe o ResourcePlanet nos planetas que ja estao na cena (sem prefabs) e, se quiseres,
// acrescenta o ResourceDelivery a nave. Pode ser corrida mais do que uma vez:
// os planetas que ja tem ResourcePlanet so sao mexidos se ligares "Overwrite Existing".
// Tudo o que acontece (e tudo o que falha) aparece na caixa de estado da janela e na Consola.
// Colocar este ficheiro numa pasta Editor.
public class PlanetSetupWindow : EditorWindow
{
    [SerializeField] private PlanetSetupSettings _settings = new PlanetSetupSettings();

    private readonly List<string> _messages = new List<string>();
    private MessageType _statusType = MessageType.None;
    private SerializedObject _serialized;
    private Vector2 _scroll;

    private void OnEnable()
    {
        _serialized = new SerializedObject(this);
        _serialized.FindProperty("_settings").isExpanded = true;
    }

    private void OnGUI()
    {
        _serialized.Update();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.PropertyField(_serialized.FindProperty("_settings"), new GUIContent("Definições"), true);
        EditorGUILayout.EndScrollView();

        _serialized.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (_messages.Count > 0)
            EditorGUILayout.HelpBox(string.Join("\n", _messages), _statusType);

        if (GUILayout.Button("Ver que planetas encontra (seleciona-os)"))
            Run(SelectFound);

        if (GUILayout.Button("1. Configurar planetas (pai / filtro de nome)"))
            Run(() => ConfigurePlanets(CollectFromScene()));

        if (GUILayout.Button("1. Configurar objetos selecionados"))
            Run(() => ConfigurePlanets(CollectSelection()));

        if (GUILayout.Button("2. Configurar entrega na nave (ResourceDelivery)"))
            Run(() => ConfigureShipDelivery());

        EditorGUILayout.Space();

        if (GUILayout.Button("Fazer tudo (1 + 2)", GUILayout.Height(28)))
            Run(DoAll);
    }

    // ---------- Estado ----------

    private void Run(System.Action action)
    {
        _messages.Clear();
        _statusType = MessageType.None;

        try
        {
            action();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            SetStatus($"Erro: {exception.Message} (detalhes na Consola)", MessageType.Error);
        }

        GUIUtility.ExitGUI();
    }

    private void SetStatus(string message, MessageType type)
    {
        _messages.Add(message);

        if (type > _statusType)
            _statusType = type;

        string text = $"Planet Setup: {message}";

        if (type == MessageType.Error)
            Debug.LogError(text);
        else if (type == MessageType.Warning)
            Debug.LogWarning(text);
        else
            Debug.Log(text);

        Repaint();
    }

    private void DoAll()
    {
        ConfigurePlanets(CollectFromScene());

        ConfigureShipDelivery();
    }

    // ---------- Procura ----------

    private List<GameObject> FindPlanetsInScene()
    {
        List<GameObject> targets = new List<GameObject>();
        Transform parent = _settings.PlanetsParent;
        string filter = _settings.NameFilter;

        if (string.IsNullOrEmpty(filter))
        {
            // Sem pai nem filtro: procura objetos com "Planet" no nome.
            if (parent == null)
                filter = "Planet";
        }

        if (string.IsNullOrEmpty(filter))
        {

            for (int i = 0; i < parent.childCount; i++)
                targets.Add(parent.GetChild(i).gameObject);

            return targets;
        }

        if (parent != null)
        {
            FindByName(parent, filter, targets);
            return targets;
        }

        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (NameMatches(root.name, filter))
                targets.Add(root);
            else
                FindByName(root.transform, filter, targets);
        }

        return targets;
    }

    private static void FindByName(Transform node, string filter, List<GameObject> targets)
    {
        for (int i = 0; i < node.childCount; i++)
        {
            Transform child = node.GetChild(i);

            // Não procura dentro de um planeta já encontrado.
            if (NameMatches(child.name, filter))
                targets.Add(child.gameObject);
            else
                FindByName(child, filter, targets);
        }
    }

    private static bool NameMatches(string objectName, string filter)
    {
        return objectName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private List<GameObject> CollectFromScene()
    {
        List<GameObject> targets = FindPlanetsInScene();

        if (targets.Count == 0)
        {
            SetStatus("Não encontrei planetas. Atribui o objeto pai (filhos diretos) ou um filtro de nome, " +
                      "ou seleciona-os na hierarquia e usa o botão dos objetos selecionados.",
                      MessageType.Warning);
        }

        return targets;
    }

    private List<GameObject> CollectSelection()
    {
        GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable | SelectionMode.ExcludePrefab);

        if (selected.Length == 0)
            SetStatus("Não há objetos da cena selecionados na hierarquia.", MessageType.Warning);

        return new List<GameObject>(selected);
    }

    private void SelectFound()
    {
        List<GameObject> found = FindPlanetsInScene();

        if (found.Count == 0)
        {
            SetStatus("Não encontrei planetas. Sem pai nem filtro procurei \"Planet\" no nome; escreve o nome certo em Name Filter " +
                      "(ex.: parte do nome dos teus planetas) ou arrasta o objeto pai para Planets Parent.", MessageType.Warning);
            return;
        }

        Selection.objects = found.ToArray();
        SetStatus($"{found.Count} planetas encontrados e selecionados na hierarquia.", MessageType.Info);
    }

    // ---------- 1. Planetas ----------

    private void ConfigurePlanets(List<GameObject> targets)
    {
        PlanetSetupSettings s = _settings;

        if (targets.Count == 0)
            return;

        int created = 0;
        int updated = 0;
        int skipped = 0;
        int withoutRenderer = 0;

        foreach (GameObject target in targets)
        {
            if (target == null)
                continue;

            ResourcePlanet planet = target.GetComponent<ResourcePlanet>();
            bool isNew = planet == null;

            if (isNew)
                planet = Undo.AddComponent<ResourcePlanet>(target);

            // Aviso "[E] Entregar" e lock de alvo (botao do meio do rato), mesmo nos planetas ja configurados.
            PlanetExtras.Ensure(target);

            if (!isNew && !s.OverwriteExisting)
            {
                skipped++;
                continue;
            }

            float radius = s.FixedRadius;

            if (s.AutoRadius)
            {
                float planetRadius = GetPlanetRadius(target);

                if (planetRadius <= 0f)
                    withoutRenderer++;

                radius = planetRadius + s.DeliveryMargin;
            }

            EditorAssetUtility.SetInt(planet, "_capacity", s.Capacity);
            EditorAssetUtility.SetFloat(planet, "_deliveryRadius", Mathf.Max(0.1f, radius));

            EditorUtility.SetDirty(planet);
            EditorSceneManager.MarkSceneDirty(target.scene);

            if (isNew)
                created++;
            else
                updated++;
        }

        SetStatus($"{created} planetas novos, {updated} atualizados, {skipped} já configurados e deixados como estavam " +
                  $"(capacidade {s.Capacity}).", MessageType.Info);

        if (withoutRenderer > 0)
        {
            SetStatus($"{withoutRenderer} planetas não têm Renderer, por isso o raio ficou só com a margem. " +
                      "Ajusta o Delivery Radius à mão ou usa o raio fixo.", MessageType.Warning);
        }
    }

    private static float GetPlanetRadius(GameObject planet)
    {
        Renderer[] renderers = planet.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
    }

    // ---------- 2. Nave ----------

    private bool ConfigureShipDelivery()
    {
        PlanetSetupSettings s = _settings;
        GameObject ship = s.Ship;

        // Sem nave atribuida: usa a que tem ResourceCollector (ou o jogador) na cena.
        if (ship == null)
        {
            ResourceCollector found = Object.FindFirstObjectByType<ResourceCollector>();
            PlayerHealth player = found == null ? Object.FindFirstObjectByType<PlayerHealth>() : null;
            ship = found != null ? found.gameObject : player != null ? player.gameObject : null;
        }

        if (ship == null)
        {
            SetStatus("Não encontrei a nave na cena (nenhum ResourceCollector nem PlayerHealth). Atribui o objeto da nave.", MessageType.Warning);
            return false;
        }

        ResourceCollector collector = ship.GetComponent<ResourceCollector>();

        if (collector == null)
        {
            SetStatus($"'{ship.name}' não tem ResourceCollector. Corre primeiro o passo 3 do Asteroid Setup.",
                MessageType.Warning);
            return false;
        }

        ResourceDelivery delivery = ship.GetComponent<ResourceDelivery>();

        if (delivery == null)
            delivery = Undo.AddComponent<ResourceDelivery>(ship);

        EditorAssetUtility.SetObject(delivery, "_collector", collector);

        if (s.DeliverClip != null)
            EditorAssetUtility.SetObject(delivery, "_deliverClip", s.DeliverClip);

        EditorUtility.SetDirty(delivery);
        EditorSceneManager.MarkSceneDirty(ship.scene);

        SetStatus($"ResourceDelivery pronto na nave '{ship.name}' (entrega com E).", MessageType.Info);
        return true;
    }
}
