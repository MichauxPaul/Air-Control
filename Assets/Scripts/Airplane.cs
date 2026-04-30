using System.Collections.Generic; 
using UnityEngine; 
using UnityEngine.InputSystem; 

public class Airplane : MonoBehaviour 
{
    [Header("Points")]
    // point de la fin de la piste
    public Transform runwayPoint;
    // point vers lequel l'avion va aller quand on va lui dire de partir ailleur
    private static Transform _goAroundPoint;  

    [Header("Movement")]
    // vitesse de base de l'avion quand il se dirige vers la piste
    public float initialSpeed = 5f;
    // distance du runwayPoint à laquelle l'avion ralentit
    public float slowRadius = 5f; 

    [Header("Taxi")]
    // vitesse au sol
    public float taxiSpeed = 1f; 

    [Header("Gate")]
    // porte assignée à l'avion
    public Transform assignedGate; 

    [Header("Visual")]
    // couleur initiale = rouge car il demande une action au joueur
    public Color startColor = Color.red;

    // états possibles de l'avion
    public enum AirplaneState 
    {
        Approach,
        WaitingAtRunway,
        Cleared,
        GoAround,
        GoingToGate,
        Parked,
        Stopped
    }

    // état actuel
    public AirplaneState state = AirplaneState.Approach;

    // composant visuel de l'avion
    private SpriteRenderer _spriteRenderer;
    // référence à l'UI
    private AirplaneUI _uiManager;
    // chemin taxi pour rejoindre la porte
    private List<Transform> _currentPath = new List<Transform>();
    // index dans le chemin
    private int _pathIndex = 0;
    // gestionnaire de taxiways
    private TaxiwayManager _taxiwayManager; 

    void Start()
    {
        // on récupère le sprite
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // on lui applique la couleur de départ couleur
        if (_spriteRenderer != null) _spriteRenderer.color = startColor;
        // on lui assigne son UI
        _uiManager = FindFirstObjectByType<AirplaneUI>(FindObjectsInactive.Include);
        // on lui assigne le taxiway manager
        _taxiwayManager = FindFirstObjectByType<TaxiwayManager>();
        // si le runwayPoint n'est pas assigné a l'avion
        if (runwayPoint == null)
        {
            // alors on le trouve dans la scène
            runwayPoint = GameObject.Find("RunwayPoint")?.transform;
        }
            

        // si le point _goAroundPoint est non défini
        if (_goAroundPoint == null) 
        {
            // on cherche le GameObjet
            GameObject obj = GameObject.Find("GoAroundPoint"); 
            if (obj != null)
            {
                // on stocke sa référence
                _goAroundPoint = obj.transform; 
            }
               
        }
    }

    void Update()
    {
        // on met à jour la couleur selon l'état dans lequel se trouve l'avion
        UpdateColor();

        // on récupère la cible (runwayPoint, _goAroundPoint, _taxiwayManager) 
        Transform target = GetTarget(); 

        // si il n'y a pas de cible, alors on sort de la boucle
        if (target == null) return;

        // direction vers la cible
        Vector3 dir = target.position - transform.position;

        // si on est pas déjà sur place
        if (dir.sqrMagnitude > 0.001f) 
        {
            // on calcule l'angle
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // on oriente l'avion
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f); 
        }

        // distance entre l'avion et la target
        float distance = Vector3.Distance(transform.position, target.position);
        // vitesse par défaut
        float speed = initialSpeed; 

        // ralentissement en approche
        if (state == AirplaneState.Approach || state == AirplaneState.Cleared)
        {
            // facteur de ralentissement
            float t = Mathf.Clamp01(distance / slowRadius);
            // on applique le ralentissement
            speed = initialSpeed * t; 
        }

        // mouvement au sol
        if (state == AirplaneState.GoingToGate)
        {
            // vitesse de l'avion pendant le taxi
            speed = taxiSpeed;

            // si l'avion est au dernier point
            if (_pathIndex == _currentPath.Count - 1) 
            {
                // on le fait ralentir  en calculant le facteur de ralentissement
                float t = Mathf.Clamp01(distance / 1.5f);
                // on applique le ralentissement
                speed = taxiSpeed * t;
            }
        }

        // déplacement autorisé sauf dans certains états
        if (state != AirplaneState.WaitingAtRunway && state != AirplaneState.Parked && state != AirplaneState.Stopped)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }

        // progression dans le chemin du  taxi
        if (state == AirplaneState.GoingToGate && _currentPath.Count > 0)
        {
            // si l'avion est proche du point
            if (distance < 0.2f) 
            {
                // on passe au point suivant
                _pathIndex++;

                // si c'est la fin du chemin
                if (_pathIndex >= _currentPath.Count) 
                {
                    // on reset le chemin
                    _currentPath.Clear();
                    // on dit que l'avion est garé
                    state = AirplaneState.Parked; 
                }
            }
        }

        // quand l'avion arrive sur la piste
        if ((state == AirplaneState.Approach || state == AirplaneState.Cleared) && distance < 0.1f)
        {
            // il attend a la fin de la piste
            state = AirplaneState.WaitingAtRunway; 
        }

        // détection du clic droit de la souris
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // position de la souris dans le monde
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            // on lance un raycast pour détecter si on clique sur un objet
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero); 

            //si on clique sur un avion
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                // on ouvre son UI
                _uiManager?.SetTarget(transform); 
            }
        }
    }

    Transform GetTarget()
    {
        if (state == AirplaneState.GoingToGate && _currentPath.Count > 0)
        {
            // on suit le chemin du taxi
            return _currentPath[_pathIndex];
        }
             

        if (state == AirplaneState.Approach || state == AirplaneState.Cleared)
        {
            // on cible la fin de la piste
            return runwayPoint;
        }
             

        if (state == AirplaneState.GoAround)
        {
            // on cible le point d'aller ailleur
            return _goAroundPoint;
        }
            
        // sinon on reste sur place
        return transform; 
    }

    public void AssignGate(Transform gate)
    {
        // on stocke la gate
        assignedGate = gate;
        // on change l'état de l'avion
        state = AirplaneState.GoingToGate;
        // on calcule le chemin
        GeneratePath(gate); 
    }

    private void GeneratePath(Transform gate)
    {
        // on reset le chemin
        _currentPath.Clear();
        // on récupère le chemin
        List<Transform> path = _taxiwayManager.GetPathForGate(gate); 
        //si il y a un chemin
        if (path != null)
        {
            // on ajoute les points
            _currentPath.AddRange(path);
            // on ajoute la destination finale
            _currentPath.Add(gate); 
        }
        // on reset index
        _pathIndex = 0; 
    }

    public void AllowLanding()
    {
        // on dit que l'avion est autorisé à l'atterrissage
        state = AirplaneState.Cleared; 
    }
    public void GoAround() 
    {
        // on dit que l'avion part ailleur
        state = AirplaneState.GoAround; 
    } 
    public void StopMovement()
    {
        // on dit que l'avion est arrêté
        state = AirplaneState.Stopped; 
    } 

    // fonction pour que l'avion reprenne son chemin vers la porte
    public void ResumeMovement()
    {
        if (assignedGate != null)
            //l'avion reprend son chemin vers la porte
            state = AirplaneState.GoingToGate; 
    }

    // fonction pour savoir si l'avion ralenti
    public bool IsSlowingDown()
    {
        // distance avec la fin de la piste
        float distance = Vector3.Distance(transform.position, runwayPoint.position);
        // vrai si l'avion est dans la zone de ralentissement
        return distance < slowRadius; 
    }

    // fonction pour changer la couleur de l'avion
    void UpdateColor()
    {
        // si l'avion est stoppé
        if (state == AirplaneState.Stopped) 
        {
            //on lui met la couleur rouge
            _spriteRenderer.color = Color.red;
            return;
        }

        // si l'avion ralenti
        if (IsSlowingDown() && state != AirplaneState.GoingToGate) 
        {
            //on lui met la couleur rouge
            _spriteRenderer.color = Color.red;
            return;
        }

        // si l'avion est en approche ou attend en fin de piste
        if (state == AirplaneState.Approach || state == AirplaneState.WaitingAtRunway) 
        {
            //on lui met la couleur rouge
            _spriteRenderer.color = Color.red;
            return;
        }

        // si l'avion est autorisé a aterrir/ a partir ailleur/ a aller a une porte/ est garé
        if (state == AirplaneState.Cleared || state == AirplaneState.GoAround || state == AirplaneState.GoingToGate || state == AirplaneState.Parked) 
        {
            //on lui met la couleur
            _spriteRenderer.color = Color.green;
            return;
        }
    }
}