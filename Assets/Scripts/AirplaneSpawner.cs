using UnityEngine; 

public class AirplaneSpawner : MonoBehaviour 
{
    // prefab de l'avion à instancier
    public GameObject planePrefab;
    // point de spawn (position de départ)
    public Transform pointA;
    // point de destination de base
    public Transform runwayPoint;
    // délai entre chaque spawn
    public float spawnDelay = 10f; 

    void Start()
    {
        // appelle la fonction SpawnPlane toutes les spawnDelay secondes
        InvokeRepeating(nameof(SpawnPlane), 0f, spawnDelay);
    }

    void SpawnPlane()
    {
        // instancie un nouvel avion à la position de pointA sans rotation
        GameObject plane = Instantiate(planePrefab, pointA.position, Quaternion.identity);

        // récupère le script Airplane attaché au prefab
        Airplane airplane = plane.GetComponent<Airplane>();

        // assigne le point de piste à l'avion
        airplane.runwayPoint = runwayPoint;
    }
}