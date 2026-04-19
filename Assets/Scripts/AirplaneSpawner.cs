using UnityEngine;

public class AirplaneSpawner : MonoBehaviour
{
    public GameObject planePrefab;
    public Transform pointA;
    public Transform runwayPoint;

    public float spawnDelay = 10f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnPlane), 0f, spawnDelay);
    }

    void SpawnPlane()
    {
        GameObject plane = Instantiate(planePrefab, pointA.position, Quaternion.identity);

        Airplane airplane = plane.GetComponent<Airplane>();
        airplane.runwayPoint = runwayPoint;
    }
}