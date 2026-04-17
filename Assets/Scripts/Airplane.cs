using UnityEngine;
using UnityEngine.InputSystem;

public class Airplane : MonoBehaviour
{
    public Transform pointB;

    [Header("Movement")]
    public float initialSpeed = 5f;   // 👈 vitesse réglable dans l'Inspector
    public float slowRadius = 5f;     // distance où il commence à ralentir

    public Color startColor = Color.red;

    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.color = startColor;
        }
    }

    void Update()
    {
        Vector3 direction = pointB.position - transform.position;

        // Rotation vers la direction
        if (direction != Vector3.zero)
        {
            transform.right = direction;
            transform.Rotate(0, 0, -90f);
        }

        // Distance au point B
        float distance = Vector3.Distance(transform.position, pointB.position);

        // 🔥 vitesse qui dépend de la distance
        float t = Mathf.Clamp01(distance / slowRadius);
        float currentSpeed = initialSpeed * t;

        // Déplacement
        transform.position = Vector3.MoveTowards(
            transform.position,
            pointB.position,
            currentSpeed * Time.deltaTime
        );

        // Si arrivé → destruction
        if (distance < 0.1f)
        {
            Destroy(gameObject);
        }

        // Détection clic
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log("Avion cliqué !");
            }
        }
    }
}