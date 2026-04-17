using UnityEngine;

public class AirplaneSpawner : MonoBehaviour
{
    public GameObject planePrefab;
    public Transform pointA;
    public Transform pointB;

    public float spawnDelay = 10f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnPlane), 0f, spawnDelay);
    }

    void SpawnPlane()
    {
        GameObject plane = Instantiate(planePrefab, pointA.position, Quaternion.identity);

        // On donne le point B à l'avion
        plane.GetComponent<Airplane>().pointB = pointB;
    }
}
