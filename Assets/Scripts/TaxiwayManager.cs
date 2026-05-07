using System.Collections.Generic;
using UnityEngine;

public class TaxiwayManager : MonoBehaviour
{
    // Liste des chemins depuis la piste vers les gates.
    public List<GatePath> GatePaths = new List<GatePath>();
    // Liste des chemins depuis une sortie de pushback vers le point d'arrêt avant le décollage.
    public List<RunwayPath> RunwayPaths = new List<RunwayPath>();
    // Chemin de décollage
    public List<Transform> TakeoffPath = new List<Transform>();

    // Fonction qui retourne une copie du chemin qui correspond a une gate donnée.
    public List<Transform> GetPathForGate(Transform gate)
    {
        // On parcourt tous les chemins configurés
        foreach (var path in GatePaths)
        {
            // Si la gate du chemin est celle demandee, on a trouvé le bon trajet.
            if (path.Gate == gate)
            {
                // On retourne une nouvelle liste pour eviter qu'on modifie l'original du manager.
                return new List<Transform>(path.Nodes);
            }
                
        }

        // Si aucun chemin trouve : l'avion ne pourra pas calculer de route vers cette gate.
        return null;
    }

    // Fonction qui retourne le chemin entre une sortie de pushback et le point d'arrêt avant le décollage.
    public List<Transform> GetPathFromPushback(Transform pushbackExitPoint)
    {
        // On parcourt les chemins de roulage vers la piste.
        foreach (var path in RunwayPaths)
        {
            // Le startPoint doit correspondre au pushbackExitPoint de la gate.
            if (path.StartPoint == pushbackExitPoint)
                // On retourne une copie pour que chaque avion ait son propre chemin a parcourir.
                return new List<Transform>(path.Nodes);
        }
        // null indique qu'aucun chemin n'a été trouvé.
        return null;
    }

    // Fonction qui retourne le chemin de décollage 
    public List<Transform> GetTakeoffPath()
    {
        // On retourne une copie pour que l'avion puisse avancer dans sa route sans changer la liste commune.
        return new List<Transform>(TakeoffPath);
    }
}
