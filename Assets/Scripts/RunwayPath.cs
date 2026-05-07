using System.Collections.Generic;
using UnityEngine;

public class RunwayPath : MonoBehaviour
{
    // Point de départ du chemin
    public Transform StartPoint;
    // Points de roulage jusqu'au point d'arrêt avant le décollage.
    public List<Transform> Nodes = new List<Transform>();
}
