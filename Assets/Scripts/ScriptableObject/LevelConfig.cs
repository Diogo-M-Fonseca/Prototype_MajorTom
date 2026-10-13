using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Game/Level Config")]
public class LevelConfig : ScriptableObject
{
    [SerializeField, Min(1)] private int _levelScore = 10;

    public int LevelScore => _levelScore;
}
