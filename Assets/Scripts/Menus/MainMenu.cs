using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public void PlayGame()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene(sceneToLoad);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
