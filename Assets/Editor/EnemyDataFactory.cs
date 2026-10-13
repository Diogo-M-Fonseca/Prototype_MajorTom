using UnityEditor;
using UnityEngine;

public readonly struct EnemyDataSet
{
    public ShipStats Stats { get; }
    public MovementProfile Movement { get; }
    public BehaviourProfile FlockProfile { get; }
    public BehaviourProfile SoloProfile { get; }
    public GunData Gun { get; }

    public EnemyDataSet(ShipStats stats, MovementProfile movement,
        BehaviourProfile flockProfile, BehaviourProfile soloProfile, GunData gun)
    {
        Stats = stats;
        Movement = movement;
        FlockProfile = flockProfile;
        SoloProfile = soloProfile;
        Gun = gun;
    }
}

public static class EnemyDataFactory
{
    // Estrutura de pastas:
    //   <pasta>/Shared/           behaviours sem parametros (Separation, Alignment, Cohesion), partilhados
    //   <pasta>/<nome>/           tudo o que e especifico deste tipo de inimigo
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

        // Perfis de comportamento
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

        return new EnemyDataSet(stats, movement, flockProfile, soloProfile, gun);
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
