using UnityEngine;
using UnityEngine.InputSystem;

public class Airplane : MonoBehaviour
{
    [Header("Points")]
    public Transform runwayPoint;
    private static Transform goAroundPoint;

    [Header("Movement")]
    public float initialSpeed = 5f;
    public float slowRadius = 5f;

    [Header("Gate")]
    public Transform assignedGate;

    [Header("Visual")]
    public Color startColor = Color.red;

    public enum AirplaneState
    {
        Approach,
        WaitingAtRunway,
        Cleared,
        GoAround,
        GoingToGate
    }

    public AirplaneState state = AirplaneState.Approach;

    private SpriteRenderer sr;
    private AirplaneUI uiManager;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.color = startColor;

        uiManager = FindFirstObjectByType<AirplaneUI>(FindObjectsInactive.Include);

        if (runwayPoint == null)
            runwayPoint = GameObject.Find("RunwayPoint")?.transform;

        if (goAroundPoint == null)
        {
            GameObject obj = GameObject.Find("GoAroundPoint");
            if (obj != null)
                goAroundPoint = obj.transform;
        }
    }

    void Update()
    {
        UpdateColor();

        Transform target = GetTarget();
        if (target == null) return;

        Vector3 dir = target.position - transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        float distance = Vector3.Distance(transform.position, target.position);
        float speed = initialSpeed;

        // 🐢 RALENTISSEMENT TOUJOURS ACTIF vers piste
        if (state == AirplaneState.Approach || state == AirplaneState.Cleared)
        {
            float t = Mathf.Clamp01(distance / slowRadius);
            speed = initialSpeed * t;
        }

        // 🚀 mouvement sauf quand il attend
        if (state != AirplaneState.WaitingAtRunway)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );
        }

        // 🛬 arrivée piste → attente joueur
        if ((state == AirplaneState.Approach || state == AirplaneState.Cleared) && distance < 0.1f)
        {
            state = AirplaneState.WaitingAtRunway;
        }

        // 🖱 sélection UI
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                uiManager?.SetTarget(transform);
            }
        }
    }

    Transform GetTarget()
    {
        switch (state)
        {
            case AirplaneState.Approach:
            case AirplaneState.Cleared:
                return runwayPoint;

            case AirplaneState.GoAround:
                return goAroundPoint;

            case AirplaneState.GoingToGate:
                return assignedGate;

            case AirplaneState.WaitingAtRunway:
                return transform; // reste sur place
        }

        return runwayPoint;
    }

    void UpdateColor()
    {
        if (sr == null) return;

        // 🐢 PRIORITÉ : ralentissement = rouge (demande joueur)
        if (IsSlowingDown() && state != AirplaneState.GoingToGate)
        {
            sr.color = Color.red;
            return;
        }

        // 🎨 sinon comportement normal
        switch (state)
        {
            case AirplaneState.Approach:
            case AirplaneState.WaitingAtRunway:
                sr.color = Color.red;
                break;

            case AirplaneState.Cleared:
                sr.color = Color.green;
                break;

            case AirplaneState.GoAround:
                sr.color = Color.green; // selon ta règle
                break;

            case AirplaneState.GoingToGate:
                sr.color = Color.green;
                break;
        }
    }

    // 🟢 autorisation (juste visuel maintenant)
    public void AllowLanding()
    {
        state = AirplaneState.Cleared;
    }

    public void GoAround()
    {
        state = AirplaneState.GoAround;
    }

    public void AssignGate(Transform gate)
    {
        assignedGate = gate;
        state = AirplaneState.GoingToGate;
    }

    // 🐢 utilisé par UI
    public bool IsSlowingDown()
    {
        if (runwayPoint == null) return false;

        float distance = Vector3.Distance(transform.position, runwayPoint.position);

        return distance < slowRadius;
    }
}