using UnityEditor;
using UnityEngine;

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
