using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class AirplaneUI : MonoBehaviour
{
    public Transform target;
    public Airplane currentAirplane;
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    [Header("UI Groups")]
    public GameObject landingButtons;

    [Header("Gates UI")]
    public GameObject gateScrollView;

    [Header("Stop Controls")]
    public GameObject stopButton;
    public GameObject resumeButton;

    [Header("Gates")]
    public Transform gatesContainer;
    public GameObject gateButtonPrefab;
    public Transform gateButtonParent;

    private bool _gatesCreated = false;
    private bool _gateSelectionLocked = false;

    [Header("Pushback")]
    public GameObject pushbackButton;

    void Start()
    {
        gameObject.SetActive(false);

        stopButton.SetActive(false);
        resumeButton.SetActive(false);
        gateScrollView.SetActive(false);
        landingButtons.SetActive(false);

        if (pushbackButton != null)
            pushbackButton.SetActive(false);
    }

    void Update()
    {
        if (target == null)
            return;

        Vector3 pos = Camera.main.WorldToScreenPoint(target.position + offset);
        transform.position = pos;

        float zoom = Camera.main.orthographicSize;
        float scale = (1f / zoom) * 5f;
        transform.localScale = Vector3.one * scale;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!IsClickInsideThisUI())
            {
                CloseUI();
                return;
            }
        }

        RefreshGateUI();
        RefreshStopResumeUI();
        RefreshLandingUI();
        RefreshPushbackUI();
    }

    public void SetTarget(Transform t)
    {
        target = t;
        currentAirplane = t.GetComponent<Airplane>();

        gameObject.SetActive(true);

        _gatesCreated = false;
        _gateSelectionLocked = currentAirplane != null && currentAirplane.assignedGate != null;

        gateScrollView.SetActive(false);

        if (pushbackButton != null)
            pushbackButton.SetActive(false);
    }

    public void CloseUI()
    {
        target = null;
        currentAirplane = null;

        gameObject.SetActive(false);

        _gateSelectionLocked = false;

        if (pushbackButton != null)
            pushbackButton.SetActive(false);
    }

    // ---------------- LANDING ----------------

    private void RefreshLandingUI()
    {
        if (currentAirplane == null)
        {
            landingButtons.SetActive(false);
            return;
        }

        if (gateScrollView.activeSelf)
        {
            landingButtons.SetActive(false);
            return;
        }

        bool showLanding = currentAirplane.state == Airplane.AirplaneState.Approach;

        landingButtons.SetActive(showLanding);
    }

    public void OnLandButton()
    {
        currentAirplane.AllowLanding();
        landingButtons.SetActive(false);
    }

    public void OnGoAroundButton()
    {
        currentAirplane.GoAround();
        landingButtons.SetActive(false);
    }

    // ---------------- STOP / RESUME ----------------

    public void OnStopButton()
    {
        currentAirplane.StopMovement();
        RefreshStopResumeUI();
    }

    public void OnResumeButton()
    {
        currentAirplane.ResumeMovement();
        RefreshStopResumeUI();
    }

    void RefreshStopResumeUI()
    {
        if (currentAirplane == null || !gameObject.activeInHierarchy || !_gateSelectionLocked)
        {
            stopButton.SetActive(false);
            resumeButton.SetActive(false);
            return;
        }

        bool isStopped = currentAirplane.state == Airplane.AirplaneState.Stopped;

        stopButton.SetActive(!isStopped);
        resumeButton.SetActive(isStopped);
    }

    // ---------------- PUSHBACK ----------------

    void RefreshPushbackUI()
    {
        if (currentAirplane == null || pushbackButton == null)
            return;

        bool show =
            currentAirplane.state == Airplane.AirplaneState.PushbackRequest;

        pushbackButton.SetActive(show);
    }

    public void OnPushbackApproved()
    {
        if (currentAirplane == null) return;

        // 🔥 déclenche le pushback réel côté avion
        currentAirplane.StartPushback();

        CloseUI();
    }

    // ---------------- GATES ----------------

    void RefreshGateUI()
    {
        if (currentAirplane == null) return;

        bool canAssignGate =
            currentAirplane.IsSlowingDown() ||
            currentAirplane.state == Airplane.AirplaneState.GoingToGate;

        if (canAssignGate && !_gateSelectionLocked)
        {
            gateScrollView.SetActive(true);

            if (!_gatesCreated)
            {
                CreateGateButtons();
                _gatesCreated = true;
            }

            return;
        }

        gateScrollView.SetActive(false);
    }

    bool IsGateOccupied(Transform gate)
    {
        Airplane[] planes = FindObjectsByType<Airplane>(FindObjectsSortMode.None);

        foreach (var p in planes)
        {
            if (p.assignedGate == gate)
                return true;
        }

        return false;
    }

    void CreateGateButtons()
    {
        foreach (Transform child in gateButtonParent)
            Destroy(child.gameObject);

        foreach (Transform gate in gatesContainer)
        {
            if (IsGateOccupied(gate))
                continue;

            Transform capturedGate = gate;

            GameObject btn = Instantiate(gateButtonPrefab, gateButtonParent);

            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = gate.name;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnGateSelected(capturedGate);
            });
        }
    }

    void OnGateSelected(Transform gate)
    {
        currentAirplane.AssignGate(gate);

        _gateSelectionLocked = true;

        HideGateUI();
        RefreshStopResumeUI();
    }

    void HideGateUI()
    {
        gateScrollView.SetActive(false);

        foreach (Transform child in gateButtonParent)
            Destroy(child.gameObject);

        _gatesCreated = false;
    }

    // ---------------- UI CLICK DETECTION ----------------

    bool IsClickInsideThisUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }
}