using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Ships/Behaviour Profile")]
public class BehaviourProfile : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [SerializeField, FormerlySerializedAs("behaviour")] private SteeringBehaviour _behaviour;
        [SerializeField, FormerlySerializedAs("weight")] private float _weight;

        public SteeringBehaviour Behaviour => _behaviour;
        public float Weight => _weight;
    }

    [SerializeField, FormerlySerializedAs("neighbourRadius")] private float _neighbourRadius = 40f;
    [SerializeField, FormerlySerializedAs("separationRadius")] private float _separationRadius = 12f;
    [SerializeField, FormerlySerializedAs("entries")] private Entry[] _entries;

    public float NeighbourRadius => _neighbourRadius;
    public float SeparationRadius => _separationRadius;
    public Entry[] Entries => _entries;
}
