using UnityEditor;
using UnityEngine;

public class LeftGame : MonoBehaviour
{
    public void leftGame()
    {
        //sortir du mode play de l'éditeur
        EditorApplication.isPlaying = false;
        //quitter le jeu "classiquement"
        Application.Quit();
    }
}
