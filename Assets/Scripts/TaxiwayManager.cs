using System.Collections.Generic; 
using UnityEngine; 

public class TaxiwayManager : MonoBehaviour 
{
    // liste des chemins disponibles
    public List<GatePath> gatePaths = new List<GatePath>(); 

    // retourne le chemin correspondant à une gate donnée
    public List<Transform> GetPathForGate(Transform gate)
    {
        // sécurité si gate invalide
        if (gate == null)
        {
            Debug.LogWarning("Gate null !");
            return null;
        }

        // parcourt tous les chemins enregistrés
        foreach (var path in gatePaths)
        {
            // si la gate correspond
            if (path.gate == gate)
            {
                // retourne la liste des points
                return path.nodes; 
            }
                
        }

        // si aucun chemin trouvé
        Debug.LogWarning("Pas de chemin pour la gate : " + gate.name);
        return null;
    }
}

// permet d'afficher dans l'inspecteur Unity
[System.Serializable] 
public class GatePath
{
    // gate associée au chemin
    public Transform gate;
    // liste des points du chemin
    public List<Transform> nodes; 
}