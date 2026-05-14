using UnityEngine;
using System.Collections;

public class AirplaneSpawner : MonoBehaviour
{
    // Prefab de l'avion a instancier.
    [SerializeField] private GameObject _planePrefab;
    // Point de départ des avions.
    [SerializeField] private Transform _pointA;
    // Point de fin de piste que les avions doivent rejoindre �l'approche.
    [SerializeField] private Transform _runwayPoint;

    [Header("Parametres du Spawner")]
    // D�lai au d�but de la partie entre deux avions.
    [SerializeField] private float _startDelay = 40f;
    // D�lai final entre deux avions quand l'intensité est maximale.
    [SerializeField] private float _endDelay = 5f;
    // Temps n�c�ssaire pour passer progressivement de _startDelay à _endDelay.
    [SerializeField] private float _timeToReachMaxIntensity = 300f;

    private void Start()
    {
        //  On fait une coroutine qui nous permet d'attendre entre deux spawns sans bloquer le jeu.
        StartCoroutine(SpawnRoutine());
    }

    // Coroutine qui fait apparaitre des avions en boucle.
    private IEnumerator SpawnRoutine()
    {
        //  Tant que la scene tourne.
        while (true)
        {
            // On créé un avion.
            SpawnPlane();

            // Progression entre 0 et 1 selon le temps écoule depuis le debut de la scène.
            float progress = Mathf.Clamp01(Time.timeSinceLevelLoad / _timeToReachMaxIntensity);

            // On fait une interpolation avec Lerp entre le delai de départ et le délai final.
            float currentDelay = Mathf.Lerp(_startDelay, _endDelay, progress);

            // On met la coroutine en pause avec WaitForSeconds avant le prochain avion.
            yield return new WaitForSeconds(currentDelay);
        }
    }

    // Fonction qui créé un avion dans la scène.
    private void SpawnPlane()
    {
        // On instantie le prefab à la position du point de départ et en lui fesant une rotation.
        GameObject plane = Instantiate(_planePrefab, _pointA.position, Quaternion.identity);
        // On r�cup�re le script Airplane sur le prefab créé.
        Airplane airplane = plane.GetComponent<Airplane>();

        // Si le script existe, on lui donne le point de fin de piste de cette scène.
        if (airplane != null)
        {
            airplane.RunwayPoint = _runwayPoint;
        }
    }
}
