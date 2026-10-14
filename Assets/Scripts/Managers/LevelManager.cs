using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Victory")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Behaviour[] gameplayBehaviourToDisableOnVictory;
    [SerializeField] private float slowDownDuration = 1.5f;

    private bool _levelCompleted;

    private void OnEnable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged += CheckLevelCompletion;
        }
    }

    private void Start()
    {
        if (levelConfig == null || scoreManager == null)
        {
            Debug.LogError(
                "LevelManager: falta atribuir LevelConfig ou ScoreManager.",
                this
            );
            return;
        }

        CheckLevelCompletion(scoreManager.CurrentScore);
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= CheckLevelCompletion;
        }
    }

    private void CheckLevelCompletion(int currentScore)
    {
        if (_levelCompleted || levelConfig == null)
            return;

        if (currentScore >= levelConfig.LevelScore)
        {
            _levelCompleted = true;
            Debug.Log("Nível concluído!");
        }
    }
}
