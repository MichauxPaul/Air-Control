using System.Collections.Generic;
using UnityEngine;

public class GatePushbackPath : MonoBehaviour
{
    [Header("Points de pushback")]
    // Liste des points que l'avion suit pendant le pushback, dans l'ordre.
    public List<Transform> PushbackPoints = new List<Transform>();

    [Header("Sortie du pushback")]
    public Transform PushbackExitPoint;
}
