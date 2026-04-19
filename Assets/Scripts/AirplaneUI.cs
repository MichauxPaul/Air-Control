using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AirplaneUI : MonoBehaviour
{
    public Transform target;
    public Airplane currentAirplane;

    public Vector3 offset = new Vector3(0, 1.5f, 0);

    [Header("UI Groups")]
    public GameObject landingButtons; // panel avec Land + GoAround
    public GameObject gateButtons;    // panel où les gates apparaissent

    [Header("Gates Auto")]
    public Transform gatesContainer;
    public GameObject gateButtonPrefab;
    public Transform gateButtonParent;

    private bool gatesCreated = false;

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (target == null) return;

        Vector3 pos = Camera.main.WorldToScreenPoint(target.position + offset);
        transform.position = pos;

        // 🔥 SCALE AVEC LE ZOOM
        float zoom = Camera.main.orthographicSize;
        float scale = (1f / zoom) * 5f;
        transform.localScale = Vector3.one * scale;

        RefreshGateUI();
    }

    public void SetTarget(Transform t)
    {
        target = t;
        currentAirplane = t.GetComponent<Airplane>();

        gameObject.SetActive(true);
        gatesCreated = false;
    }

    public void OnLandButton()
    {
        if (currentAirplane == null) return;

        currentAirplane.AllowLanding();
    }

    public void OnGoAroundButton()
    {
        currentAirplane?.GoAround();
    }

    public void CloseUI()
    {
        target = null;
        currentAirplane = null;
        gameObject.SetActive(false);
    }

    // 🚪 GATES AUTO
    void RefreshGateUI()
    {
        if (currentAirplane == null) return;

        // ✈️ avant autorisation
        if (currentAirplane.state == Airplane.AirplaneState.Approach)
        {
            landingButtons.SetActive(true);
            gateButtons.SetActive(false);
            return;
        }

        // 🟢 autorisé MAIS PAS encore en ralentissement
        if (currentAirplane.state == Airplane.AirplaneState.Cleared &&
            !currentAirplane.IsSlowingDown())
        {
            landingButtons.SetActive(false);
            gateButtons.SetActive(false);
            return;
        }

        // 🐢 ralentissement → afficher gates
        if (currentAirplane.IsSlowingDown())
        {
            landingButtons.SetActive(false);
            gateButtons.SetActive(true);

            if (!gatesCreated)
            {
                CreateGateButtons();
                gatesCreated = true;
            }

            return;
        }

        // 🚪 déjà assigné
        if (currentAirplane.state == Airplane.AirplaneState.GoingToGate)
        {
            landingButtons.SetActive(false);
            gateButtons.SetActive(false);
        }
    }

    void CreateGateButtons()
    {
        if (gateButtonParent == null || gatesContainer == null || gateButtonPrefab == null)
        {
            Debug.LogError("UI Gates mal configurée !");
            return;
        }

        foreach (Transform child in gateButtonParent)
            Destroy(child.gameObject);

        Transform[] gates = gatesContainer.GetComponentsInChildren<Transform>();

        int index = 0;

        foreach (Transform gate in gates)
        {
            if (gate == gatesContainer) continue;

            Transform capturedGate = gate;

            GameObject btn = Instantiate(gateButtonPrefab, gateButtonParent);

            var text = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
                text.text = "Gate " + (index + 1);

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                currentAirplane.AssignGate(capturedGate);
            });

            index++;
        }
    }
}