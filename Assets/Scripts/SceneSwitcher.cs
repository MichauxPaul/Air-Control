using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestion des scènes
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    /// <summary>
    /// permet de changer de scène dans l'application
    /// </summary>
    /// <param name="sceneName">Nom d'une scène qui est présente dans la liste</param>
    public void SwitchScene (string sceneName)
    {
        SceneManager.LoadScene (sceneName);
    }
}
