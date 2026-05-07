using System.Collections.Generic;
using UnityEngine;

public class GatePath : MonoBehaviour
{
    // Porte associée a ce chemin.
    public Transform Gate;
    // Points que l'avion suit pour atteindre cette porte.
    public List<Transform> Nodes = new List<Transform>();
}
