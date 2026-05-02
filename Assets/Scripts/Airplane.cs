using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Airplane : MonoBehaviour
{
    [Header("Points")]
    public Transform runwayPoint;
    private static Transform _goAroundPoint;

    [Header("Movement")]
    public float initialSpeed = 5f;
    public float slowRadius = 5f;

    [Header("Taxi")]
    public float taxiSpeed = 1f;

    [Header("Pushback")]
    public float pushbackSpeed = 1f;

    [Header("Gate")]
    public Transform assignedGate;

    [Header("Visual")]
    public Color startColor = Color.white;

    // 🔊 AUDIO
    [Header("Audio")]
    public AudioClip clickSound;
    private AudioSource _audioSource;

    public enum AirplaneState
    {
        Approach,
        SlowingApproach,
        Cleared,
        Touchdown,
        WaitingAtRunway,

        GoAround,

        GoingToGate,
        AtGateWaitingPushback,

        PushbackRequest,
        Pushback,

        Parked,
        Stopped
    }

    public AirplaneState state = AirplaneState.Approach;

    private SpriteRenderer _spriteRenderer;
    private AirplaneUI _uiManager;
    private TaxiwayManager _taxiwayManager;

    private List<Transform> _taxiPath = new List<Transform>();
    private int _taxiIndex = 0;

    private List<Transform> _pushbackPath = new List<Transform>();
    private int _pushbackIndex = 0;

    private float _gateTimer;
    private float _gateDelay;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>(); // 🔊

        if (_spriteRenderer != null)
        {
            startColor = Color.white;
            _spriteRenderer.color = Color.red;
        }

        _uiManager = FindFirstObjectByType<AirplaneUI>(FindObjectsInactive.Include);
        _taxiwayManager = FindFirstObjectByType<TaxiwayManager>();

        if (runwayPoint == null)
            runwayPoint = GameObject.Find("RunwayPoint")?.transform;

        if (_goAroundPoint == null)
        {
            GameObject obj = GameObject.Find("GoAroundPoint");
            if (obj != null)
                _goAroundPoint = obj.transform;
        }
    }

    void Update()
    {
        HandleStateTransitions();
        HandleGateTimer();
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

        if (state == AirplaneState.Pushback)
        {
            HandlePushback();
            return;
        }

        float speed = GetSpeed(distance);

        if (CanMove())
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                speed * Time.deltaTime
            );
        }

        HandleTaxi(distance);
        HandleRunwayArrival(distance);
        HandleClick();
    }

    void HandleStateTransitions()
    {
        if ((state == AirplaneState.Approach || state == AirplaneState.Cleared)
            && IsSlowingDown())
        {
            state = AirplaneState.SlowingApproach;
        }
    }

    float GetSpeed(float distance)
    {
        if (state == AirplaneState.Approach ||
            state == AirplaneState.SlowingApproach)
        {
            float t = Mathf.Clamp01(distance / slowRadius);
            return initialSpeed * t;
        }

        if (state == AirplaneState.GoingToGate)
            return taxiSpeed;

        if (state == AirplaneState.Pushback)
            return pushbackSpeed;

        return initialSpeed;
    }

    bool CanMove()
    {
        return state != AirplaneState.WaitingAtRunway &&
               state != AirplaneState.Parked &&
               state != AirplaneState.Stopped &&
               state != AirplaneState.AtGateWaitingPushback;
    }

    void HandleRunwayArrival(float distance)
    {
        if ((state == AirplaneState.Approach ||
             state == AirplaneState.SlowingApproach ||
             state == AirplaneState.Cleared)
            && distance < 0.1f)
        {
            state = AirplaneState.WaitingAtRunway;
        }
    }

    void HandleTaxi(float distance)
    {
        if (state != AirplaneState.GoingToGate) return;
        if (_taxiPath.Count == 0) return;

        if (distance < 0.2f)
        {
            _taxiIndex++;

            if (_taxiIndex >= _taxiPath.Count)
            {
                _taxiPath.Clear();
                StartGateWait();
            }
        }
    }

    void StartGateWait()
    {
        state = AirplaneState.AtGateWaitingPushback;
        _gateTimer = 0f;
        _gateDelay = Random.Range(30f, 60f);
    }

    void HandleGateTimer()
    {
        if (state != AirplaneState.AtGateWaitingPushback) return;

        _gateTimer += Time.deltaTime;

        if (_gateTimer >= _gateDelay)
            state = AirplaneState.PushbackRequest;
    }

    public void SetPushbackPath(List<Transform> path)
    {
        _pushbackPath = path != null ? new List<Transform>(path) : new List<Transform>();
        _pushbackIndex = 0;
    }

    public void StartPushback()
    {
        state = AirplaneState.Pushback;
        _pushbackIndex = 0;
    }

    void HandlePushback()
    {
        if (_pushbackPath.Count == 0)
        {
            Debug.LogWarning("❌ Aucun chemin de pushback !");
            state = AirplaneState.Parked;
            return;
        }

        Transform target = _pushbackPath[_pushbackIndex];

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            pushbackSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, target.position) < 0.2f)
        {
            _pushbackIndex++;

            if (_pushbackIndex >= _pushbackPath.Count)
            {
                state = AirplaneState.Parked;
            }
        }
    }

    // 🔊 CLICK AVEC SON
    void HandleClick()
    {
        if (state == AirplaneState.AtGateWaitingPushback)
            return;

        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                // 🔊 joue le son
                if (_audioSource != null && clickSound != null)
                    _audioSource.PlayOneShot(clickSound);

                _uiManager?.SetTarget(transform);
            }
        }
    }

    Transform GetTarget()
    {
        if (state == AirplaneState.Pushback && _pushbackPath.Count > 0)
            return _pushbackPath[_pushbackIndex];

        if (state == AirplaneState.GoingToGate && _taxiPath.Count > 0)
            return _taxiPath[_taxiIndex];

        if (state == AirplaneState.Approach ||
            state == AirplaneState.SlowingApproach ||
            state == AirplaneState.Cleared)
            return runwayPoint;

        if (state == AirplaneState.GoAround)
            return _goAroundPoint;

        return transform;
    }

    public void AssignGate(Transform gate)
    {
        assignedGate = gate;
        state = AirplaneState.GoingToGate;

        GeneratePath(gate);

        var pushback = gate.GetComponent<GatePushbackPath>();
        if (pushback != null)
        {
            SetPushbackPath(pushback.pushbackPoints);
        }
    }

    void GeneratePath(Transform gate)
    {
        _taxiPath.Clear();

        List<Transform> path = _taxiwayManager.GetPathForGate(gate);

        if (path != null)
        {
            _taxiPath.AddRange(path);
            _taxiPath.Add(gate);
        }

        _taxiIndex = 0;
    }

    public void AllowLanding() => state = AirplaneState.Cleared;
    public void GoAround() => state = AirplaneState.GoAround;
    public void StopMovement() => state = AirplaneState.Stopped;

    public void ResumeMovement()
    {
        if (assignedGate != null)
            state = AirplaneState.GoingToGate;
    }

    public bool IsSlowingDown()
    {
        return Vector3.Distance(transform.position, runwayPoint.position) < slowRadius;
    }

    void UpdateColor()
    {
        if (_spriteRenderer == null) return;

        Color col = startColor;

        if (state == AirplaneState.Approach ||
            state == AirplaneState.SlowingApproach ||
            state == AirplaneState.WaitingAtRunway ||
            state == AirplaneState.PushbackRequest ||
            state == AirplaneState.Stopped ||
            state == AirplaneState.Parked)
        {
            col = Color.red;
        }
        else if (state == AirplaneState.Cleared ||
                 state == AirplaneState.GoingToGate ||
                 state == AirplaneState.Pushback ||
                 state == AirplaneState.GoAround)
        {
            col = Color.green;
        }
        else if (state == AirplaneState.AtGateWaitingPushback)
        {
            col = startColor;
        }

        _spriteRenderer.color = col;
    }
}