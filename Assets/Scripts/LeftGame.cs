using UnityEngine;

public class LeftGame : MonoBehaviour
{
    public void QuitGame()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            PlateformeClient.Close();
            return;
        }

        Application.Quit();
    }
}
