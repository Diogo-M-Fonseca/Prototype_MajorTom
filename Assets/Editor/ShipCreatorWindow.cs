// ShipCreatorWindow.cs
// Colocar em Assets/Editor/ (a pasta tem de se chamar "Editor").
// Abre em: Tools > Ship Creator
//
// Uma so janela para:
//   Inimigo : criar/atualizar todos os dados (stats, movimento, behaviours, perfis, FSM de estados, arma) + prefab
//   Jogador : criar/atualizar o prefab do jogador (com movimento e input ja configurados)
//   Cena    : criar pools de projeteis, colocar o jogador (com a camara a segui-lo) e os inimigos (individuais ou em bando)
//
// Depende de: EditorAssetUtility, EnemyDataFactory, EnemyDataSettings, ShipPrefabUtility,
// PlayerMovementSettings, CameraFollowSettings (+ SpaceshipController, ShipInputAdapter, CameraFollow)
// (substitui o EnemyCreatorWindow, que podes apagar).
using System.Collections.Generic;
using SpaceFlight;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipCreatorWindow : EditorWindow
{
    private enum Tab { Enemy, Player, Scene }
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
    [SerializeField] private string _enemyPrefabFolder = "Assets/Enemies";

    // ---- Jogador
    [SerializeField] private string _playerName = "Player";
    [SerializeField] private string _playerFolder = "Assets/Player";
    [SerializeField] private GameObject _playerModel;
    [SerializeField] private ShipColliderType _playerCollider = ShipColliderType.Box;
    [SerializeField] private bool _playerRigidbody = true;
    [SerializeField] private float _playerMaxHealth = 100f;
    [SerializeField] private ShipStats _playerStats;
    [SerializeField] private GunData _playerGun;
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

    // ---- Opcoes comuns de prefab
    [SerializeField] private bool _replaceModel;
    [SerializeField] private bool _rebuildCollider;

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

    [MenuItem("Tools/Ship Creator")]
    private static void Open()
    {
        GetWindow<ShipCreatorWindow>("Ship Creator");
    }

    private void OnEnable()
    {
        minSize = new Vector2(440f, 520f);
        FindInputActions(false);
        _so = new SerializedObject(this);
        _so.FindProperty("_settings").isExpanded = true;
        DetectLayers();
    }

    private void DetectLayers()
    {
        if (_playerLayer == 0) _playerLayer = Mathf.Max(0, LayerMask.NameToLayer(PlayerLayerName));
        if (_enemyLayer == 0) _enemyLayer = Mathf.Max(0, LayerMask.NameToLayer(EnemyLayerName));
        if (_obstacleLayer == 0) _obstacleLayer = Mathf.Max(0, LayerMask.NameToLayer(ObstacleLayerName));
    }

    // =============================================================== GUI

    private void OnGUI()
    {
        if (_so == null) _so = new SerializedObject(this);
        _so.Update();

        DrawHeader();

        _tab = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Inimigo", "Jogador", "Cena" });
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        switch (_tab)
        {
            case Tab.Enemy: DrawEnemyTab(); break;
            case Tab.Player: DrawPlayerTab(); break;
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
            Prop("_cameraSettings", "Parametros da camara");

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
        Prop("_playerGun", "Gun Data");
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
                Transform muzzle = ShipPrefabUtility.GetOrCreateFiringPoint(root);
                GunController gun = GetOrAdd<GunController>(root);

                EditorAssetUtility.SetObject(gun, "gunData", _playerGun);
                EditorAssetUtility.SetObject(gun, "firingPoint", muzzle);

                if (_playerShootClip != null)
                {
                    AudioSource source = GetOrAdd<AudioSource>(root);
                    source.playOnAwake = false;

                    EditorAssetUtility.SetObject(gun, "audioSource", source);
                    EditorAssetUtility.SetObject(gun, "shootClip", _playerShootClip);
                }
            }

            if (_playerMovement)
                BuildPlayerMovement(root);

            AddExtraScripts(root);
            ShipPrefabUtility.SetLayerRecursively(root, _playerLayer);

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

        ShipInputAdapter input = GetOrAdd<ShipInputAdapter>(root);
        EditorAssetUtility.SetObject(input, "ship", ship);
        EditorAssetUtility.SetObject(input, "move", _moveAction);
        EditorAssetUtility.SetObject(input, "look", _lookAction);
        EditorAssetUtility.SetBool(input, "lockCursor", _movement.LockCursor);
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

        GunController gun = instance.GetComponent<GunController>();
        if (gun != null && _playerPool != null)
            EditorAssetUtility.SetObject(gun, "projectilePool", _playerPool);
        else if (gun != null)
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

        // Posiciona a camara ja em modo de edicao, como o CameraFollow faria no arranque.
        Vector3 forward = target.forward * (invertForward ? -1f : 1f);
        Vector3 position = target.position - forward * _cameraSettings.Distance + target.up * _cameraSettings.Height;
        Vector3 lookPoint = target.position + forward * _cameraSettings.LookAheadDistance;

        Undo.RecordObject(cameraObject.transform, "Setup camera");
        cameraObject.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(lookPoint - position, target.up));

        Debug.Log($"Ship Creator: camara '{cameraObject.name}' a seguir '{target.name}'.", cameraObject);
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
