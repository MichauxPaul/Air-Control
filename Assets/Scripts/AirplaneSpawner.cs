using UnityEngine;
using System.Collections;

public class AirplaneSpawner : MonoBehaviour
{
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform runwayPoint;

    [Header("Paramètres de Spawn")]
    [SerializeField] private float startDelay = 40f;      // Délai au début
    [SerializeField] private float endDelay = 5f;        // Délai après 5 mins
    [SerializeField] private float timeToReachMaxIntensity = 300f; // 5 minutes en secondes

    private float timer;

    void Start()
    {
        // On lance la routine de spawn
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnPlane();

            // Calcul du ratio de progression (entre 0 et 1)
            // Time.timeSinceLevelLoad donne le temps écoulé depuis le début de la scène
            float progress = Mathf.Clamp01(Time.timeSinceLevelLoad / timeToReachMaxIntensity);

            // Interpolation linéaire entre le délai de départ et le délai de fin
            float currentDelay = Mathf.Lerp(startDelay, endDelay, progress);

            // On attend le délai calculé avant le prochain spawn
            yield return new WaitForSeconds(currentDelay);
        }
    }

    void SpawnPlane()
    {
        GameObject plane = Instantiate(planePrefab, pointA.position, Quaternion.identity);
        Airplane airplane = plane.GetComponent<Airplane>();

        if (airplane != null)
        {
            airplane.runwayPoint = runwayPoint;
        }
    }
}