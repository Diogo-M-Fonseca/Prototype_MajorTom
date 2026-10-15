using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelConfig levelConfig;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Victory")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Behaviour[] gameplayBehaviourToDisableOnVictory;
    [SerializeField] private float slowDownDuration = 1.5f;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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
            CompleteLevel();
        }
    }

    private void CompleteLevel()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        _levelCompleted = true;

        foreach (Behaviour behaviour in gameplayBehaviourToDisableOnVictory)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        float elapsedTime = 0f;
        float initialTimeScale = Time.timeScale;

        while (elapsedTime < slowDownDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            
            float progress = elapsedTime / slowDownDuration;
            Time.timeScale = Mathf.Lerp(initialTimeScale, 0f, progress);

            yield return null;
        }

        Time.timeScale = 0f;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
