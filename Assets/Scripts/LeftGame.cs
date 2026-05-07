using UnityEditor;
using UnityEngine;

public class LeftGame : MonoBehaviour
{
    public void QuitGame()
    {
        // On arrête le mode Play.
        EditorApplication.isPlaying = false;
        // On ferme l'application.
        Application.Quit();
    }
}
