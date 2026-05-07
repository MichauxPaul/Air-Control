using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Airplane : MonoBehaviour
{
    [Header("Points")]
    // Point que l'avion vise pendant l'approche.
    public Transform RunwayPoint;
    // Point de sortie utilise quand le joueur refuse l'atterrissage.
    private static Transform _goAroundPoint;

    [Header("Vitesse d'approche")]
    // Vitesse pendant l'arrivée.
    public float InitialSpeed = 5f;
    // Distance à partir de laquelle l'avion ralentit avant la fin de la piste.
    public float SlowRadius = 5f;

    [Header("Vitesse durant le Taxi")]
    public float TaxiSpeed = 1f;

    [Header("Vitesse durant le Pushback")]
    public float PushbackSpeed = 1f;

    [Header("Takeoff")]
    public float MaxTakeoffSpeed = 8f;
    // Accélération appliquée jusqu'à la vitesse maximale.
    public float TakeoffAcceleration = 1f;

    [Header("Gate")]
    public Transform AssignedGate;

    [Header("Visual")]
    public Color StartColor = Color.white;

    [Header("Audio")]
    // Son joué quand on clique sur l'avion.
    public AudioClip ClickSound;
    // AudioSource qui joue le son.
    private AudioSource _audioSource;

    // Enumère tous les états possibles de l'avion.
    public enum AirplaneState
    {
        Approach,
        SlowingApproach,
        Cleared,
        WaitingAtRunway,
        GoAround,
        GoingToGate,
        AtGate,
        PushbackRequest,
        Pushback,
        TaxiRequest,
        TaxiingToRunway,
        TakeoffRequest,
        TakingOff,
        TakeoffComplete,
        Parked,
        Stopped
    }

    // Etat de base de l'avion.
    public AirplaneState State = AirplaneState.Approach;

    // Ici SpriteRenderer va nous servir a changer la couleur de l'avion.
    private SpriteRenderer _spriteRenderer;
    // Référence vers l'UI
    private AirplaneUI _uiManager;
    // Référence vers le gestionnaire des chemins.
    private TaxiwayManager _taxiwayManager;

    // Chemin utilisé pour rouler au sol.
    private List<Transform> _taxiPath = new List<Transform>();
    // Index du prochain point dans le chemin de taxi.
    private int _taxiIndex = 0;

    // Chemin utilise pendant le pushback.
    private List<Transform> _pushbackPath = new List<Transform>();
    // Index du prochain point de pushback.
    private int _pushbackIndex = 0;
    // Point de sortie du pushback, on l'utilise pour trouver le bon chemin vers la piste.
    private Transform _pushbackExitPoint;

    // Index du prochain point dans le chemin de décollage.
    private int _takeoffIndex = 0;
    // Vitesse courante pendant le decollage; elle augmente progressivement.
    private float _currentTakeoffSpeed;
    // On récupère le chemin du décollage fourni par TaxiwayManager.
    private List<Transform> _takeoffPath = new List<Transform>();

    // Timer utilise quand l'avion attend a la gate.
    private float _gateTimer;
    // Délai aléatoire avant que l'avion demande le pushback.
    private float _gateDelay;

    // On mémorise l'état avant un stop pour savoir ou reprendre.
    private AirplaneState _stateBeforeStop;
    // On évite de donner plusieurs fois les points d'arrivée a la gate.
    private bool _gateArrivalScored;
    // On évite de donner plusieurs fois les points de décollage.
    private bool _takeoffScored;

    // Propriété publique pour savoir si l'avion est actuellement arrêté.
    public bool IsStopped() 
    {
        return State == AirplaneState.Stopped;
    }
     

    private void Start()
    {
        // On récupère le SpriteRenderer de l'avion.
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // On récupère l'AudioSource de l'avion.
        _audioSource = GetComponent<AudioSource>();

        // Si l'avion a un sprite, on lui applique la couleur rouge de départ.
        if (_spriteRenderer != null)
        {
            _spriteRenderer.color = Color.red;
        }

        // Cherche l'UI des avions
        _uiManager = FindFirstObjectByType<AirplaneUI>(FindObjectsInactive.Include);
        // Cherche le gestionnaire des chemins dans la scène.
        _taxiwayManager = FindFirstObjectByType<TaxiwayManager>();

        // Si RunwayPoint n'est pas assigné, on essaie de le trouver par son nom.
        if (RunwayPoint == null)
            RunwayPoint = GameObject.Find("RunwayPoint").transform;

        // Si le point de go around n'est pas assigné on essaie de le trouver par son nom
        if (_goAroundPoint == null)
        {
            _goAroundPoint = GameObject.Find("GoAroundPoint").transform;
        }
    }


    private void Update()
    {
        // Gère les transitions d'état automatiquement.
        UpdateStateTransitions();
        // Gère l'attente a la gate.
        UpdateGateTimer();
        // Met à jour la couleur selon l'état.
        UpdateColor();

        // On récupère la cible actuelle de l'avion.
        Transform target = GetTarget();
        // Si aucune cible n'existe, on ne peut pas bouger.
        if (target == null) return;

        // Direction entre l'avion et sa cible.
        Vector3 dir = target.position - transform.position;

        // Si la direction est assez grande, on oriente l'avion vers sa cible.
        if (dir.sqrMagnitude > 0.001f)
        {
            // Atan2 calcule l'angle en radiant entre l'avion et sa cible. On la convertie ensuite en degrès
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            // On applique une rotation pour bien orienter le sprite.
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // Distance entre l'avion et sa cible.
        float distance = Vector3.Distance(transform.position, target.position);

        // Logique de mouvement du pushback.
        if (State == AirplaneState.Pushback)
        {
            // Gère le recul de l'avion.
            MovePushback();
            // On quitte la fonction pour éviter que le mouvement normal s'ajoute au pushback.
            return;
        }

        // Le decollage a aussi sa propre logique car il accelère progressivement.
        if (State == AirplaneState.TakingOff)
        {
            // Gère l'accélération et le chemin de décollage.
            MoveTakeoff();
            // On quitte pour ne pas utiliser le mouvement normal.
            return;
        }

        // On calcule la vitesse selon l'état et la distance.
        float speed = GetSpeed(distance);

        // Si l'état permet le mouvement, on avance vers la cible.
        if (CanMove())
        {
            // La fonction MoveTowards deplace l'avion sans dépasser la cible.
            transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        }

        // Gère le passage d'un point de taxi au suivant.
        UpdateTaxiPath(distance);
        // Gère l'arrivée sur la piste ou au go around.
        CheckRunwayArrival(distance);
        // Gère le clic sur l'avion.
        CheckClickSelection();
    }

    // Gère les transitions entre les états.
    private void UpdateStateTransitions()
    {
        // Si l'avion est en approche et entre dans le rayon de ralentissement.
        if ((State == AirplaneState.Approach || State == AirplaneState.Cleared) && IsSlowingDown())
        {
            // Il passe en ralentissement.
            State = AirplaneState.SlowingApproach;
        }
    }

    // Retourne la vitesse adaptée a l'état actuel.
    private float GetSpeed(float distance)
    {
        // Pendant l'approche, on ralentit progressivement près de la fin de piste.
        if (State == AirplaneState.Approach || State == AirplaneState.SlowingApproach)
        {
            // on garde une valeur entre 0 et 1.
            float t = Mathf.Clamp01(distance / SlowRadius);
            // Plus l'avion est proche, plus la vitesse baisse.
            return InitialSpeed * t;
        }

        // Le roulage vers une gate ou vers la piste utilise TaxiSpeed.
        if (State == AirplaneState.GoingToGate || State == AirplaneState.TaxiingToRunway)
        {
            return TaxiSpeed;
        }
            

        // Le pushback utilise sa propre vitesse.
        if (State == AirplaneState.Pushback)
        {
            return PushbackSpeed;
        }
            

        // Sinon on utilise la vitesse normale.
        return InitialSpeed;
    }

    // On dit si l'avion a le droit de bouger avec le mouvement standard.
    private bool CanMove()
    {
        // On interdit le mouvement dans les états d'attente ou d'arrêt.
        return State != AirplaneState.WaitingAtRunway && State != AirplaneState.Parked && State != AirplaneState.Stopped && State != AirplaneState.AtGate;
    }

    // Gère l'arrivée a la piste et le go around.
    private void CheckRunwayArrival(float distance)
    {
        // Si l'avion arrive au RunwayPoint pendant l'approche.
        if ((State == AirplaneState.Approach || State == AirplaneState.SlowingApproach || State == AirplaneState.Cleared) && distance < 0.1f)
        {
            // Il attend une affectation de gate.
            State = AirplaneState.WaitingAtRunway;
        }

        // Si l'avion atteint son point de go around.
        if (State == AirplaneState.GoAround && distance < 0.2f)
        {
            // On le detruit 
            Destroy(gameObject);
        }
    }

    // Gère le roulage sur les chemins au sol.
    private void UpdateTaxiPath(float distance)
    {
        // On traite seulement les deux états de roulage.
        if (State != AirplaneState.GoingToGate && State != AirplaneState.TaxiingToRunway) 
        {
            return;
        }

        // S'il n'y a aucun chemin, on ne fait rien.
        if (_taxiPath.Count == 0) 
        {
            return;
        } 

        // Si l'avion est assez proche du point courant.
        if (distance < 0.2f)
        {
            // On passe au point suivant.
            _taxiIndex++;

            // Si on a depassé le dernier point du chemin.
            if (_taxiIndex >= _taxiPath.Count)
            {
                // On vide le chemin courant.
                _taxiPath.Clear();

                // Si l'avion allait a une gate, il commence son attente a la porte.
                if (State == AirplaneState.GoingToGate)
                {
                    StartGateWait();
                }

                // Si l'avion allait à la piste, il demande maintenant le decollage.
                else if (State == AirplaneState.TaxiingToRunway) 
                {
                    State = AirplaneState.TakeoffRequest;
                }
                    
            }
        }
    }

    // Fonction pour lancer l'attente a la gate.
    private void StartGateWait()
    {
        // L'avion est maintenant a la porte.
        State = AirplaneState.AtGate;
        // Remet le timer a zero.
        _gateTimer = 0f;
        // Délai aléatoire avant la demande de pushback (entre 30 seconde et 1 minute).
        _gateDelay = Random.Range(30f, 60f);

        // Si les points de score de gate n'ont pas encore été donnés.
        if (!_gateArrivalScored)
        {
            // On ajoute les points dans le GameManager.
            GameManager.Instance.AddGateArrivalPoints();
            // On mémorise que les points ont été donnés.
            _gateArrivalScored = true;
        }
    }

    // G*ère le timer d'attente a la gate.
    private void UpdateGateTimer()
    {
        // Si l'avion n'attend pas a la gate, on quitte.
        if (State != AirplaneState.AtGate) 
        {
            return;
        } 

        // On ajoute le temps écoule depuis la dernière frame.
        _gateTimer += Time.deltaTime;

        // Quand le timer atteint le delai.
        if (_gateTimer >= _gateDelay) 
        {
            // L'avion demande le pushback.
            State = AirplaneState.PushbackRequest;
        }
            
    }

    // On définit le chemin de pushback.
    public void SetPushbackPath(List<Transform> path)
    {
        // Si path existe, on l'utilise; sinon on créé une liste vide.
        if (path != null)
        {
            _pushbackPath = new List<Transform>(path);
        }
        else
        {
            _pushbackPath = new List<Transform>();
        }
    
        // On commence au premier point.
        _pushbackIndex = 0;
    }

    // Fonction pour lancer le pushback après autorisation.
    public void StartPushback()
    {
        // La porte est liberée dès le depart du pushback.
        AssignedGate = null;
        // Si la liste des portes est ouverte, elle est mise à jour.
        _uiManager.RefreshGateButtonsIfOpen();

        // Passe en état pushback.
        State = AirplaneState.Pushback;
        // Recommence au premier point de pushback.
        _pushbackIndex = 0;
    }

    // Fonction pour déplacer l'avion sur son chemin de pushback.
    private void MovePushback()
    {
        // Si aucun chemin n'est configuré.
        if (_pushbackPath.Count == 0)
        {
            // On met l'avion dans un état pour éviter un blocage.
            State = AirplaneState.Parked;
            // On quitte la fonction.
            return;
        }

        // Cible actuelle du pushback.
        Transform target = _pushbackPath[_pushbackIndex];

        // Avance vers le point de pushback courant avec sa vitesse propre.
        transform.position = Vector3.MoveTowards(transform.position, target.position, PushbackSpeed * Time.deltaTime
        );

        // Si l'avion est arrive au point courant.
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            // Passe au point suivant.
            _pushbackIndex++;

            // Si le pushback est fini.
            if (_pushbackIndex >= _pushbackPath.Count)
            {
                // L'avion demande l'autorisation de rouler vers la piste.
                State = AirplaneState.TaxiRequest;
            }
        }
    }

    // Fonction pour géré le clic sur l'avion et jouer un son en même temps.
    private void CheckClickSelection()
    {
        // Quand l'avion est a la gate, on ne veut pas ouvrir son UI.
        if (State == AirplaneState.AtGate) 
        {
            return;
        }

        // Si le clic gauche vient d'être préssé.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // On convertit la position souris en position monde.
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            // On lance un Raycast pour vérifié sur quel collider est la souris.
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            // Si le raycast touche cet avion.
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                // Si un son et une source audio existent, on joue le son de clic.
                if (_audioSource != null && ClickSound != null) 
                {
                    _audioSource.PlayOneShot(ClickSound);
                }
                    
                // On ouvre l'UI sur cet avion.
                _uiManager.SetTarget(transform);
            }
        }
    }

    // Fonction qui retourne la cible actuelle selon l'état.
    private Transform GetTarget()
    {
        // Pendant le pushback, la cible est le point de pushback courant.
        if (State == AirplaneState.Pushback && _pushbackPath.Count > 0) 
        {
             return _pushbackPath[_pushbackIndex];
        }


        // Pendant le décollage, la cible est le point de décollage courant.
        if (State == AirplaneState.TakingOff && _takeoffPath.Count > 0) 
        {
            return _takeoffPath[_takeoffIndex];
        }
            

        // Pendant le taxi, la cible est le point de taxi courant.
        if ((State == AirplaneState.GoingToGate || State == AirplaneState.TaxiingToRunway) && _taxiPath.Count > 0)
        {
            // On retourne le prochain point du chemin de taxi.
            return _taxiPath[_taxiIndex];
        }

        // Pendant l'approche, l'avion vise le RunwayPoint.
        if (State == AirplaneState.Approach || State == AirplaneState.SlowingApproach || State == AirplaneState.Cleared) 
        { 
            return RunwayPoint;
        }


        // Pendant un go around, l'avion vise le point de go around.
        if (State == AirplaneState.GoAround) 
        {
            return _goAroundPoint;
        }
            

        // Par defaut, l'avion vise sa propre position
        return transform;
    }

    // Fonction pour assigner une gate a l'avion.
    public void AssignGate(Transform gate)
    {
        // On mémorise la gate choisie.
        AssignedGate = gate;
        // L'avion commence à rouler vers cette gate.
        State = AirplaneState.GoingToGate;

        // Génère le chemin vers la gate.
        GeneratePath(gate);

        // On récupère le chemin de pushback configuré sur cette gate.
        var pushback = gate.GetComponent<GatePushbackPath>();
        // Si la gate a un script de pushback.
        if (pushback != null)
        {
            // On assigne les points de pushback à l'avion.
            SetPushbackPath(pushback.PushbackPoints);
            // On mémorise la sortie de pushback pour le futur chemin vers la piste.
            _pushbackExitPoint = pushback.PushbackExitPoint;
        }
    }

    // Fonction qui génère le chemin vers une gate.
    private void GeneratePath(Transform gate)
    {
        // On vide l'ancien chemin.
        _taxiPath.Clear();

        // On demande au manager le chemin vers cette gate.
        List<Transform> path = _taxiwayManager.GetPathForGate(gate);

        // Si un chemin existe.
        if (path != null)
        {
            // On ajoute les points intermediaires.
            _taxiPath.AddRange(path);
            // On ajoute la gate comme dernier point.
            _taxiPath.Add(gate);
        }

        // On repart au premier point.
        _taxiIndex = 0;
    }

    // Fonction pour générer le chemin depuis la sortie de pushback vers le point d'arrêt.
    private void GenerateRunwayPath()
    {
        // On vide l'ancien chemin.
        _taxiPath.Clear();

        // Si aucun point de sortie n'est assigne, on utilise le dernier point de pushback.
        if (_pushbackExitPoint == null && _pushbackPath.Count > 0) 
        {
            _pushbackExitPoint = _pushbackPath[_pushbackPath.Count - 1];
        }
            

        // On demande au manager le chemin correspondant a cette sortie de pushback.
        List<Transform> path = _taxiwayManager.GetPathFromPushback(_pushbackExitPoint);

        // Si un chemin existe, on l'ajoute.
        if (path != null) 
        {
            _taxiPath.AddRange(path);
        }

        // On repart au premier point.
        _taxiIndex = 0;
    }

    // Fonction pour lancer le roulage vers la piste.
    private void StartTaxiToRunway()
    {
        // On Créé le chemin vers la piste.
        GenerateRunwayPath();

        // Si aucun chemin n'a ete trouve.
        if (_taxiPath.Count == 0)
        {
            // On place l'avion en état Parked pour eviter qu'il reste bloqué.
            State = AirplaneState.Parked;
            // On quitte la fonction.
            return;
        }

        // L'avion roule vers le point d'arrêt.
        State = AirplaneState.TaxiingToRunway;
    }

    // Fonction qui autorise le roulage vers la piste.
    public void AllowTaxiToRunway()
    {
        // On n'autorise cette action que si l'avion la demandé.
        if (State != AirplaneState.TaxiRequest) 
        {
            return;
        } 

        // On lance le roulage vers la piste.
        StartTaxiToRunway();
    }

    // Fonction qui autorise le décollage.
    public void AllowTakeoff()
    {
        // On n'autorise cette action que si l'avion attend le décollage.
        if (State != AirplaneState.TakeoffRequest) 
        {
            return;
        } 

        // On repart au premier point de décollage.
        _takeoffIndex = 0;
        // On remet la vitesse de décollage à zéro pour accélérer progressivement.
        _currentTakeoffSpeed = 0f;
        // On récupère le chemin de décollage
        _takeoffPath = _taxiwayManager.GetTakeoffPath();

        // Si aucun chemin de decollage n'est configuré.
        if (_takeoffPath.Count == 0)
        {
            // On considère que le décollage est terminé.
            State = AirplaneState.TakeoffComplete;
            // On donne quand même les points de decollage.
            ScoreTakeoff();
            // On quitte la fonction.
            return;
        }

        // L'avion commence son décollage.
        State = AirplaneState.TakingOff;
    }

    // Fonction qui gère le mouvement de décollage avec l'accélération.
    private void MoveTakeoff()
    {
        // Si aucun chemin n'existe, on termine.
        if (_takeoffPath.Count == 0)
        {
            // Etat final du décollage.
            State = AirplaneState.TakeoffComplete;
            // On quitte la fonction.
            return;
        }

        // Point de décollage courant.
        Transform target = _takeoffPath[_takeoffIndex];

        // On accélère progressivement jusqu'a MaxTakeoffSpeed.
        _currentTakeoffSpeed = Mathf.MoveTowards(_currentTakeoffSpeed, MaxTakeoffSpeed, TakeoffAcceleration * Time.deltaTime);

        // On avance vers le point courant avec la vitesse actuelle.
        transform.position = Vector3.MoveTowards(transform.position, target.position, _currentTakeoffSpeed * Time.deltaTime);

        // Si l'avion atteint le point courant.
        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            // On passe au point suivant.
            _takeoffIndex++;

            // Si le dernier point est atteint.
            if (_takeoffIndex >= _takeoffPath.Count)
            {
                // On passe en état de décollage terminé.
                State = AirplaneState.TakeoffComplete;
                // On garde l'index sur le dernier point validé.
                _takeoffIndex = _takeoffPath.Count - 1;
                // On donne les points du décollage.
                ScoreTakeoff();
                // On détruit l'avion pour éviter des problèmes
                Destroy(gameObject);
            }
        }
    }

    // Fonction qui donne les points de décollage
    private void ScoreTakeoff()
    {
        // Si les points ont deja été donnés, on évite de les redonner.
        if (_takeoffScored) 
        {
            return;
        }
        

        // On ajoute les points dans le GameManager.
        GameManager.Instance.AddTakeoffPoints();
        // On mémorise que les points ont été donnés.
        _takeoffScored = true;
    }

    // Fonction pour autorisé l'atterrissage.
    public void AllowLanding() 
    {
        State = AirplaneState.Cleared;
    }
    
    // Fonction pour mettre l'avion en go around.
    public void GoAround() 
    { 
        State = AirplaneState.GoAround; 
    }

    // FOnction qui stoppe le roulage si possible.
    public void StopMovement()
    {
        // Si l'avion n'est pas dans un état stoppable, on ne fait rien.
        if (!CanStopMovement()) 
        {
            return;
        }
        

        // On mémorise l'état actuel pour reprendre le bon roulage.
        _stateBeforeStop = State;
        // On passe en état stoppe.
        State = AirplaneState.Stopped;
    }

    // Fonction qui reprend le roulage après un stop.
    public void ResumeMovement()
    {
        // Si l'avion était en roulage vers une gate ou la piste, on reprend cet état.
        if (_stateBeforeStop == AirplaneState.GoingToGate || _stateBeforeStop == AirplaneState.TaxiingToRunway)
        {
            State = _stateBeforeStop;
        }
        // Si l'avion avait une gate, il reprend vers la gate.
        else if (AssignedGate != null)
        {
            State = AirplaneState.GoingToGate;
        }
    }

    // FOnction qui indique si le bouton Stop peut apparaitre.
    public bool CanStopMovement()
    {
        // On peut stopper seulement les roulages.
        return State == AirplaneState.GoingToGate || State == AirplaneState.TaxiingToRunway;
    }

    // Fonction qui indique si l'avion est dans la zone de ralentissement.
    public bool IsSlowingDown()
    {
        // On compare la distance entre l'avion et le RunwayPoint au rayon de ralentissement.
        return Vector3.Distance(transform.position, RunwayPoint.position) < SlowRadius;
    }

    // Fonction qui indique si l'avion est considéré comme étant a la porte.
    private bool IsAtGate()
    {
        // Ces etats ne doivent pas causer de game over en cas de contact.
        return State == AirplaneState.AtGate || State == AirplaneState.PushbackRequest;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // GetComponentInParent marche meme si le collider est sur un enfant de l'avion.
        Airplane other = collision.gameObject.GetComponentInParent<Airplane>();
        // On gère la collision avec l'autre avion trouvé.
        CheckAirplaneCollision(other);
    }

    // Collision 2D avec un collider configure en trigger.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // On récupère l'avion sur l'objet touche ou son parent.
        Airplane otherAirplane = other.GetComponentInParent<Airplane>();
        // On gère la collision avec l'autre avion trouvé.
        CheckAirplaneCollision(otherAirplane);
    }

    // Fonction qui décide si une collision entre deux avions cause un game over.
    private void CheckAirplaneCollision(Airplane other)
    {
        // Si l'autre objet n'est pas un avion, on l'ignore.
        if (other == null) 
        {
            return;
        }
        // Si Unity nous renvoie nous-meme, on ignore.
        if (other == this) 
        {
            return;
        }

        // Si un des avions est a la porte, on ignore la collision sinon impossible d'avoir tout les avion a toutes les gate.
        if (IsAtGate() || other.IsAtGate()) 
        {
            return;
        } 

        // Sinon, deux avions se touchent = game over.
        GameManager.Instance.GameOver();
    }

    // Fonction qui met à jour la couleur de l'avion selon son état.
    private void UpdateColor()
    {
        // Si aucun SpriteRenderer n'existe, impossible de changer la couleur.
        if (_spriteRenderer == null) 
        {
            return;
        }
        

        // Couleur par defaut de l'avion.
        Color col = StartColor;

        // Rouge signifie que l'avion attend une action de la part du joueur.
        if (State == AirplaneState.Approach || State == AirplaneState.SlowingApproach || State == AirplaneState.WaitingAtRunway || State == AirplaneState.PushbackRequest || State == AirplaneState.TaxiRequest || State == AirplaneState.TakeoffRequest || State == AirplaneState.Stopped || State == AirplaneState.Parked)
        {
            // On applique le rouge.
            col = Color.red;
        }
        // Vert signifie que l'avion est autorisé et en train de suivre une action.
        else if (State == AirplaneState.Cleared || State == AirplaneState.GoingToGate || State == AirplaneState.TaxiingToRunway || State == AirplaneState.Pushback || State == AirplaneState.TakingOff || State == AirplaneState.TakeoffComplete || State == AirplaneState.GoAround)
        {
            // On applique le vert.
            col = Color.green;
        }
        // A la porte, l'avion reprend sa couleur de base.
        else if (State == AirplaneState.AtGate)
        {
            // On applique la couleur de base.
            col = StartColor;
        }

        // On applique la couleur finale au sprite.
        _spriteRenderer.color = col;
    }
}
