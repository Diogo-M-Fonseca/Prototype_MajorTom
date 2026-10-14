using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private string sceneToLoad;

    public void PlayGame()
    {
        SceneManager.LoadScene(sceneToLoad);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
