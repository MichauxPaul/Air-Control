using TMPro; 
using UnityEngine; 
using UnityEngine.EventSystems; 
using UnityEngine.UI; 
using UnityEngine.InputSystem; 
using System.Collections.Generic; 

public class AirplaneUI : MonoBehaviour
{
    // avion ciblé
    public Transform target;
    // script de l'avion sélectionné
    public Airplane currentAirplane;
    // décalage du UI au-dessus de l'avion
    public Vector3 offset = new Vector3(0, 1.5f, 0); 

    [Header("UI Groups")]
    // panel boutons de l'atterrissage
    public GameObject landingButtons; 

    [Header("Gates UI")]
    // scroll des portes
    public GameObject gateScrollView; 

    [Header("Stop Controls")]
    // bouton stop
    public GameObject stopButton;
    // bouton reprise
    public GameObject resumeButton; 

    [Header("Gates")]
    // liste des gates
    public Transform gatesContainer;
    // prefab du bouton des gates
    public GameObject gateButtonPrefab;
    // parent des boutons des gates
    public Transform gateButtonParent;
    // on évite la recréation des boutons
    private bool _gatesCreated = false;
    // on empêche de pouvoir re-sélectioner une porte
    private bool _gateSelectionLocked = false; 

    void Start()
    {
        // UI cachée au départ
        gameObject.SetActive(false);
        // on cache le bouton stop
        stopButton.SetActive(false);
        // on cache le bouton resume
        resumeButton.SetActive(false);
        // on cache l'affichage des gates
        gateScrollView.SetActive(false);
        // on cache les boutons landing
        landingButtons.SetActive(false); 
    }

    void Update()
    {
        // si rien n'est sélectionner, on affiche rien
        if (target == null) 
        {
            return; 
        } 

        // on positionne l'UI au-dessus de l'avion
        Vector3 pos = Camera.main.WorldToScreenPoint(target.position + offset);
        transform.position = pos;

        // on adapte la taille selon zoom caméra
        float zoom = Camera.main.orthographicSize;
        float scale = (1f / zoom) * 5f;
        transform.localScale = Vector3.one * scale;

        // clic gauche de la souris
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // si le clic est hors de l'UI
            if (!IsClickInsideThisUI()) 
            {
                // on le ferme
                CloseUI(); 
                return;
            }
        }

        // on met à jour tous les panels
        RefreshGateUI();
        RefreshStopResumeUI();
        RefreshLandingUI();
    }

    
    public void SetTarget(Transform t)
    {
        // on stocke la cible
        target = t;
        // on récupère le script de l'avion
        currentAirplane = t.GetComponent<Airplane>();
        // on affiche l'UI
        gameObject.SetActive(true);
        // on reset les boutons des gates
        _gatesCreated = false;
        // verrou si gate déjà assignée
        _gateSelectionLocked = currentAirplane != null && currentAirplane.assignedGate != null;
        // on cache les gates au départ
        gateScrollView.SetActive(false); 
    }

    public void CloseUI()
    {
        // on reset la cible
        target = null; 
        currentAirplane = null;
        // on cache l'UI
        gameObject.SetActive(false);
        // on reset les boutons des gates
        _gateSelectionLocked = false; 
    }

    private void RefreshLandingUI()
    {
        if (currentAirplane == null)
        {
            landingButtons.SetActive(false);
            return;
        }

        // si les gates sont visibles alors on cache landing
        if (gateScrollView.activeSelf)
        {
            landingButtons.SetActive(false);
            return;
        }

        // afficher uniquement l'UI landing en approche
        bool showLanding = currentAirplane.state == Airplane.AirplaneState.Approach;

        landingButtons.SetActive(showLanding);
    }

    public void OnLandButton()
    {
        // autorise atterrissage
        currentAirplane.AllowLanding();
        // on cache le panel
        landingButtons.SetActive(false); 
    }

    public void OnGoAroundButton()
    {
        // aller ailleur
        currentAirplane.GoAround();
        // on cache le panel
        landingButtons.SetActive(false); 
    }

    public void OnStopButton()
    {
        // stopper l'avion
        currentAirplane.StopMovement();
        // update l'UI
        RefreshStopResumeUI(); 
    }

    public void OnResumeButton()
    {
        // reprend mouvement
        currentAirplane.ResumeMovement(); 
        RefreshStopResumeUI();
    }

    void RefreshStopResumeUI()
    {
        // conditions pour cacher les boutons
        if (currentAirplane == null || !gameObject.activeInHierarchy || !_gateSelectionLocked)
        {
            stopButton.SetActive(false);
            resumeButton.SetActive(false);
            return;
        }

        // vérifie si avion est stoppé
        bool isStopped = currentAirplane.state == Airplane.AirplaneState.Stopped;
        // stop visible si pas stoppé
        stopButton.SetActive(!isStopped);
        // resume visible si stoppé
        resumeButton.SetActive(isStopped); 
    }

    void RefreshGateUI()
    {
        if (currentAirplane == null) return;

        // conditions pour afficher les gates
        bool canAssignGate = currentAirplane.IsSlowingDown() || currentAirplane.state == Airplane.AirplaneState.GoingToGate;

        if (canAssignGate && !_gateSelectionLocked)
        {
            // affiche scroll
            gateScrollView.SetActive(true); 

            if (!_gatesCreated)
            {
                // crée boutons
                CreateGateButtons(); 
                _gatesCreated = true;
            }

            return;
        }
        // sinon cacher
        gateScrollView.SetActive(false); 
    }

    bool IsGateOccupied(Transform gate)
    {
        // récupère tous les avions
        Airplane[] planes = FindObjectsByType<Airplane>(FindObjectsSortMode.None); 

        foreach (var p in planes)
        {
            // si gate déjà assignée
            if (p.assignedGate == gate)
            {
                return true;
            }
        }
        return false;
    }

    void CreateGateButtons()
    {
        // supprime anciens boutons
        foreach (Transform child in gateButtonParent)
        {
            Destroy(child.gameObject);
        }
            

        // parcourt toutes les gates
        foreach (Transform gate in gatesContainer)
        {
            // skip si occupée
            if (IsGateOccupied(gate))
            {
                continue;
            }

            // capture pour lambda
            Transform capturedGate = gate;
            // crée bouton
            GameObject btn = Instantiate(gateButtonPrefab, gateButtonParent);
            // récupère texte
            var text = btn.GetComponentInChildren<TextMeshProUGUI>(); 
            if (text != null)
            {
                // affiche nom
                text.text = gate.name; 
            }
                

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                // assignation
                OnGateSelected(capturedGate); 
            });
        }
    }

    void OnGateSelected(Transform gate)
    {
        // assigne gate
        currentAirplane.AssignGate(gate);
        // bloque sélection
        _gateSelectionLocked = true;
        // cache UI
        HideGateUI();
        // update boutons stop/resume
        RefreshStopResumeUI(); 
    }

    void HideGateUI()
    {
        // cache scroll
        gateScrollView.SetActive(false); 

        foreach (Transform child in gateButtonParent)
        {
            // supprime boutons
            Destroy(child.gameObject); 
        }

        // reset création
        _gatesCreated = false; 
    }

    bool IsClickInsideThisUI()
    {
        // crée event
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        // position souris
        eventData.position = Mouse.current.position.ReadValue(); 

        var results = new List<RaycastResult>();
        // raycast UI
        EventSystem.current.RaycastAll(eventData, results); 

        foreach (var r in results)
        {
            // si dans UI
            if (r.gameObject.transform.IsChildOf(transform))
            {
                return true;
            } 
                
        }
        return false;
    }
}