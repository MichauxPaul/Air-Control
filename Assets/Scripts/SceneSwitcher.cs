using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void SwitchScene (string sceneName)
    {
        if (sceneName == "Game" && GameManager.Instance != null)
        {
            GameManager.Instance.PlayGame();
        }

        SceneManager.LoadScene (sceneName);
    }
}
