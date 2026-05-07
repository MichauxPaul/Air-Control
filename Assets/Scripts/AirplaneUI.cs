using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AirplaneUI : MonoBehaviour
{
    // Avion que l'UI doit suivre.
    public Transform Target;
    // Script Airplane associe a la cible.
    public Airplane CurrentAirplane;
    // Décalage visuel pour afficher l'UI un peu au-dessus de l'avion.
    public Vector3 Offset = new Vector3(0, 1.5f, 0);

    [Header("UI Groups")]
    public GameObject LandingButtons;
    // ScrollView qui contient les boutons de selection de porte.
    public GameObject GateScrollView;

    [Header("Stop Controls")]
    public GameObject StopButton;
    public GameObject ResumeButton;

    [Header("Gates")]
    // Parent qui contient toutes les gates de la scène.
    public Transform GatesContainer;
    // Prefab de bouton utilisé pour chaque gate disponible.
    public GameObject GateButtonPrefab;
    // Parent UI dans lequel les boutons de gate sont instancies.
    public Transform GateButtonParent;

    // Indique si les boutons de gate ont déjà ete créés pour éviter de les recréer chaque frame.
    private bool _gatesCreated = false;
    // Indique si l'avion a déjà une gate assignée.
    private bool _gateSelectionLocked = false;

    [Header("Pushback")]
    public GameObject PushbackButton;

    [Header("Taxi")]
    public GameObject TaxiButton;

    [Header("Takeoff")]
    public GameObject TakeoffButton;

    private void Start()
    {
        // L'UI commence cachée tant qu'aucun avion n'est sélectionné.
        gameObject.SetActive(false);

        // On cache le bouton stop au départ.
        StopButton.SetActive(false);
        // On cache le bouton reprise au départ.
        ResumeButton.SetActive(false);
        // On cache la liste des gates au départ.
        GateScrollView.SetActive(false);
        // On cache les boutons d'atterrissage au départ.
        LandingButtons.SetActive(false);

        // Si le bouton pushback est assigné, on le cache.
        if (PushbackButton != null)
            PushbackButton.SetActive(false);

        // Si le bouton taxi est assigné, on le cache.
        if (TaxiButton != null)
            TaxiButton.SetActive(false);

        // Si le bouton decollage est assigné, on le cache.
        if (TakeoffButton != null)
            TakeoffButton.SetActive(false);
    }

    private void Update()
    {
        // Si aucune cible n'est selectionnée, l'UI n'a rien a suivre.
        if (Target == null) 
        {
            return;
        }
            

        // On convertit la position monde de l'avion en position écran pour placer l'UI.
        Vector3 pos = Camera.main.WorldToScreenPoint(Target.position + Offset);
        // On applique la position calculée à l'UI.
        transform.position = pos;

        // On récupère le zoom de la caméra.
        float zoom = Camera.main.orthographicSize;
        // On calcule une échelle inverse au zoom pour garder l'UI lisible.
        float scale = (1f / zoom) * 5f;
        // On applique l'échelle à l'UI.
        transform.localScale = Vector3.one * scale;

        // Si le joueur clique gauche cette frame.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Si le clic n'est pas dans cette UI, on ferme le panneau.
            if (!IsClickInsideThisUI())
            {
                CloseUI();
                // On quitte Update pour ne pas rafraichir une UI fermée.
                return;
            }
        }

        // On met à jour l'affichage des gates.
        RefreshGateUI();
        // On met à jour les boutons stop/reprise.
        RefreshStopResumeUI();
        // On met à jour les boutons d'atterrissage.
        RefreshLandingUI();
        // On met à jour le bouton pushback.
        RefreshPushbackUI();
        // On met à jour le bouton d'autorisation de roulage.
        RefreshTaxiUI();
        // On met à jour le bouton d'autorisation de decollage.
        RefreshTakeoffUI();
    }

    // Fonction qui sélectionne un avion comme cible de l'UI.
    public void SetTarget(Transform t)
    {
        // On mémorise le Transform de l'avion.
        Target = t;
        // On récupère le script Airplane sur cet objet.
        CurrentAirplane = t.GetComponent<Airplane>();

        // On affiche l'UI.
        gameObject.SetActive(true);

        // On force la régénération des boutons de gate si nécessaire.
        _gatesCreated = false;
        // On vérrouille la selection si l'avion a déjà une gate.
        _gateSelectionLocked = CurrentAirplane != null && CurrentAirplane.AssignedGate != null;

        // On cache la liste des gates au moment de l'ouverture.
        GateScrollView.SetActive(false);

        // On cache le bouton pushback à l'ouverture.
        if (PushbackButton != null) 
        {
            PushbackButton.SetActive(false);
        }


        // On cache le bouton taxi à l'ouverture.
        if (TaxiButton != null) 
        {
            TaxiButton.SetActive(false);
        }
            

        // On cache le bouton décollage à l'ouverture.
        if (TakeoffButton != null) 
        {
            TakeoffButton.SetActive(false); 
        }
            
    }

    // Fonction qui ferme l'UI et oublie la cible.
    public void CloseUI()
    {
        // On oublie le Transform de l'avion.
        Target = null;
        // On oublie le script Airplane de l'avion.
        CurrentAirplane = null;

        // On cache l'UI.
        gameObject.SetActive(false);

        // On déverrouille la sélection de gate pour la prochaine ouverture.
        _gateSelectionLocked = false;

        // On cache le bouton pushback.
        if (PushbackButton != null) 
        {
            PushbackButton.SetActive(false);
        }


        // On cache le bouton taxi.
        if (TaxiButton != null) 
        {
            TaxiButton.SetActive(false);
        }


        // On cache le bouton de décollage.
        if (TakeoffButton != null) 
        {
            TakeoffButton.SetActive(false);
        }
            
    }

    // Fonction qui affiche ou cache les boutons d'atterrissage.
    private void RefreshLandingUI()
    {
        // Si aucun avion n'est sélectionné, on cache les boutons.
        if (CurrentAirplane == null)
        {
            LandingButtons.SetActive(false);
            // On quitte la fonction.
            return;
        }

        // Si la liste des gates est ouverte, on cache les boutons landing pour éviter les conflits d'UI.
        if (GateScrollView.activeSelf)
        {
            LandingButtons.SetActive(false);
            // On quitte la fonction.
            return;
        }

        // Les boutons landing s'affichent seulement quand l'avion est en approche.
        bool showLanding = CurrentAirplane.State == Airplane.AirplaneState.Approach;

        // On applique l'affichage.
        LandingButtons.SetActive(showLanding);
    }

    // Fonction appelé par le bouton d'autorisation d'atterrissage
    public void ApproveLanding()
    {
        // On autorise l'avion à atterrir.
        CurrentAirplane.AllowLanding();
        // On cache les boutons landing après le clic.
        LandingButtons.SetActive(false);
    }

    // Fonction appelé par le bouton de go around
    public void SendGoAround()
    {
        // On envoie l'avion vers le point de go around.
        CurrentAirplane.GoAround();
        // On cache les boutons landing après le clic.
        LandingButtons.SetActive(false);
    }

    //  Fonction appelé par le bouton Stop.
    public void StopRoulage()
    {
        // On demande à l'avion de stopper son roulage.
        CurrentAirplane.StopMovement();
        // On met les boutons à jour.
        RefreshStopResumeUI();
    }

    // Fonction appelé par le bouton de reprise du roulage
    public void ResumeRoulage()
    {
        // On demande à l'avion de reprendre son roulage.
        CurrentAirplane.ResumeMovement();
        // On met les boutons à jour.
        RefreshStopResumeUI();
    }

    // Fonction qui affiche Stop ou Reprise selon l'état de l'avion.
    private void RefreshStopResumeUI()
    {
        // Si aucun avion n'est sélectionné, on cache les deux boutons.
        if (CurrentAirplane == null || !gameObject.activeInHierarchy)
        {
            // On cache le bouton Stop.
            StopButton.SetActive(false);
            // On cache le bouton Reprise.
            ResumeButton.SetActive(false);
            // On quitte la fonction.
            return;
        }

        // On demande à l'avion s'il est actuellement arrêté.
        bool isStopped = CurrentAirplane.IsStopped();
        // On demande à l'avion s'il est dans un état ou le stop est permis.
        bool canStop = CurrentAirplane.CanStopMovement();

        // Stop s'affiche seulement si l'avion peut être arrêté et n'est pas déjà arrêté.
        StopButton.SetActive(canStop && !isStopped);
        // Reprise s'affiche seulement quand l'avion est arrêté.
        ResumeButton.SetActive(isStopped);
    }

    // Fonction qui affiche ou cache le bouton pushback.
    private void RefreshPushbackUI()
    {
        // Si aucun avion ou aucun bouton n'est assigne, on ne fait rien.
        if (CurrentAirplane == null || PushbackButton == null) 
        {
            return;
        }
            

        // On affiche le bouton pushback quand l'avion demande le pushback.
        bool show = CurrentAirplane.State == Airplane.AirplaneState.PushbackRequest;

        // On applique l'affichage.
        PushbackButton.SetActive(show);
    }

    // Fonction appelé par le bouton d'autorisation pushback.
    public void ApprovePushback()
    {
        // Si aucun avion n'est selectionné, on sort de la fonction.
        if (CurrentAirplane == null) 
        {
            return;
        } 

        // On lance le pushback côté avion.
        CurrentAirplane.StartPushback();

        // On ferme l'UI après l'action.
        CloseUI();
    }

    // Fonction qui affiche ou cache le bouton d'autorisation de roulage vers la piste.
    private void RefreshTaxiUI()
    {
        // Si aucun avion ou aucun bouton n'est assigne, on ne fait rien.
        if (CurrentAirplane == null || TaxiButton == null) 
        {
            return;
        }
            

        // Le bouton taxi apparait quand l'avion demande le roulage.
        bool show = CurrentAirplane.State == Airplane.AirplaneState.TaxiRequest;

        // On applique l'affichage.
        TaxiButton.SetActive(show);
    }

    // Fonction appelé par le bouton d'autorisation de roulage.
    public void ApproveTaxi()
    {
        //  Si aucun avion n'est selectionné, on ne fait rien.
        if (CurrentAirplane == null) 
        {
            return;
        } 

        // On autorise l'avion à rouler vers la piste.
        CurrentAirplane.AllowTaxiToRunway();

        // On ferme l'UI après l'action.
        CloseUI();
    }

    // Fonction qui affiche ou cache le bouton d'autorisation de décollage.
    private void RefreshTakeoffUI()
    {
        // Si aucun avion ou aucun bouton n'est assigné, on ne fait rien.
        if (CurrentAirplane == null || TakeoffButton == null) 
        {
            return;
        }
            

        // Le bouton décollage apparait quand l'avion demande le decollage.
        bool show = CurrentAirplane.State == Airplane.AirplaneState.TakeoffRequest;

        // On applique l'affichage.
        TakeoffButton.SetActive(show);
    }

    // Fonction appelé par le bouton d'autorisation de décollage.
    public void ApproveTakeoff()
    {
        // Securite si aucun avion n'est selectionné, on ne fait rien.
        if (CurrentAirplane == null) 
        {
            return;
        } 

        // On lance la séquence de décollage de l'avion.
        CurrentAirplane.AllowTakeoff();

        // On ferme l'UI après l'action.
        CloseUI();
    }

    // Fonction qui affiche ou cache la liste des portes disponibles.
    private void RefreshGateUI()
    {
        // Si aucun avion n'est selectionné, on ne fait rien.
        if (CurrentAirplane == null) 
        {
            return;
        } 

        // On peut assigner une gate quand l'avion ralentit près de la fin de la piste ou se dirige déjà vers une gate.
        bool canAssignGate = CurrentAirplane.IsSlowingDown() || CurrentAirplane.State == Airplane.AirplaneState.GoingToGate;

        // Si on peut assigner une gate et que la gate n'est pas déjà verrouillée.
        if (canAssignGate && !_gateSelectionLocked)
        {
            // On affiche la liste des gates.
            GateScrollView.SetActive(true);

            // On créé les boutons une seule fois tant que la liste reste ouverte.
            if (!_gatesCreated)
            {
                // On génère les boutons de gates disponibles.
                CreateGateButtons();
                // On mémorise que les boutons existent.
                _gatesCreated = true;
            }

            // On quitte pour ne pas cacher la liste juste après.
            return;
        }

        // Si on ne peut pas assigner, on cache la liste.
        GateScrollView.SetActive(false);
    }

    // Fonction qui vérifie si une gate est déjà occupée par un avion.
    private bool IsGateOccupied(Transform gate)
    {
        // On cherche tous les avions actifs dans la scene.
        Airplane[] planes = FindObjectsByType<Airplane>(FindObjectsSortMode.None);

        // On parcourt tous les avions trouvés.
        foreach (var p in planes)
        {
            // Si un avion a déjà cette gate d'assignée, alors elle est occupée.
            if (p.AssignedGate == gate) 
            {
                return true;
            }
                
        }

        // Sinon aucun avion n'utilise cette gate.
        return false;
    }

    // Fonction qui créé les boutons correspondant aux gates disponibles.
    private void CreateGateButtons()
    {
        // On supprime les anciens boutons pour reconstruire une liste propre.
        foreach (Transform child in GateButtonParent) 
        {
            Destroy(child.gameObject);
        }
            

        // On parcourt toutes les gates placées dans le container.
        foreach (Transform gate in GatesContainer)
        {
            // On ignore les gates déjà occupées.
            if (IsGateOccupied(gate)) 
            {
                continue;
            }
                

            // Variable locale pour que le listener garde la bonne gate.
            Transform capturedGate = gate;

            // On créé un bouton a partir du prefab.
            GameObject btn = Instantiate(GateButtonPrefab, GateButtonParent);

            // On récupère le texte dans le bouton.
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            // Si le texte existe, on affiche le nom de la gate.
            if (text != null) 
            {
                text.text = gate.name;
            }
                

            // On ajoute une action au clic du bouton.
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                // Quand ce bouton est cliqué, on assigne la gate cliqué.
                SelectGate(capturedGate);
            });
        }
    }

    // Fonction qui reconstruit les boutons de gates si la liste est ouverte.
    public void RefreshGateButtonsIfOpen()
    {
        // Si l'UI est fermée, il n'y a rien a mettre à jour.
        if (!gameObject.activeInHierarchy) 
        {
            return;
        }
        // Si aucun avion n'est selectionné, il n'y a rien a mettre à jour.
        if (CurrentAirplane == null) 
        {
            return;
        }
        // Si la selection est vérrouillée, on ne doit plus changer la liste.
        if (_gateSelectionLocked) 
        {
            return;
        }
        // Si la liste n'est pas visible, on ne reconstruit rien.
        if (!GateScrollView.activeSelf) 
        {
            return;
        } 

        // On eeconstruit les boutons selon les gates maintenant disponibles.
        CreateGateButtons();
        // On mémorise que les boutons sont créés.
        _gatesCreated = true;
    }

    // Fonction appelé quand le joueur choisit une gate.
    private void SelectGate(Transform gate)
    {
        // On donne la gate à l'avion.
        CurrentAirplane.AssignGate(gate);

        // On vérrouille pour éviter de réassigner une deuxieme gate au même avion.
        _gateSelectionLocked = true;

        // On cache la liste et on détruit les boutons.
        HideGateUI();
        // On met à jour le Stop/Reprise, car l'avion roule vers une gate.
        RefreshStopResumeUI();
    }

    // Fonction qui cache la liste des gates et supprime les boutons.
    private void HideGateUI()
    {
        // On cache le scroll view.
        GateScrollView.SetActive(false);

        // On détruit tous les boutons générés.
        foreach (Transform child in GateButtonParent) 
        {
            Destroy(child.gameObject);
        }
            

        // On indique que les boutons devront être recréés la prochaine fois.
        _gatesCreated = false;
    }

    // Fonction qui vérifie si le clic souris est dans cette UI.
    private bool IsClickInsideThisUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        // On donne au système UI la position actuelle de la souris.
        eventData.position = Mouse.current.position.ReadValue();

        // On créé une liste qui recevra tous les éléments UI touchés par le raycast.
        var results = new List<RaycastResult>();
        // On demande quels éléments sont sous la souris.
        EventSystem.current.RaycastAll(eventData, results);

        // On parcourt tous les resultats du raycast UI.
        foreach (var r in results)
        {
            // Si l'element touché est un enfant de cette UI, le clic est dedans.
            if (r.gameObject.transform.IsChildOf(transform)) 
            {
                return true;
            }
                
        }

        // Sinon aucun élément de cette UI n'a été touché.
        return false;
    }
}
