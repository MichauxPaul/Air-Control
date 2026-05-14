using UnityEngine;
using UnityEngine.InputSystem;

public class CamFollowMouse : MonoBehaviour
{
    [Header("Drag")]
    // Vitesse a laquelle la caméra se déplace pendant le drag.
    public float DragSpeed = 1f;

    [Header("Zoom")]
    // Vitesse de zoom avec la molette.
    public float ZoomSpeed = 10f;
    // Zoom minimum autorisé.
    public float MinZoom = 5f;
    // Zoom maximum autorisé.
    public float MaxZoom = 20f;

    [Header("Limits")]
    // Limites horizontales de la caméra.
    public Vector2 LimitX = new Vector2(-50, 50);
    // Limites verticales de la caméra.
    public Vector2 LimitY = new Vector2(-50, 50);

    // Position du clic au début du drag.
    private Vector3 _dragOrigin;
    // On indique si le joueur est en train de déplacer la caméra.
    private bool _isDragging;

    // Référence vers la caméra principale.
    private Camera _cam;

    private void Start()
    {
        // On cherche la caméra marquée avec le tag MainCamera.
        _cam = Camera.main;
    }

    private void Update()
    {
        // On gère le déplacement avec le clic droit.
        UpdateDrag();
        // On gère le zoom avec la molette.
        UpdateZoom();
        // On garde la caméra dans les limites.
        ClampPosition();
    }

    // Fonction qui gère le deplacement de la caméra.
    private void UpdateDrag()
    {
        // Si le clic droit vient d'etre appuyé, on mémorise la position de départ.
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // On convertit une position écran en position monde avec ScreenToWorldPoint.
            _dragOrigin = _cam.ScreenToWorldPoint(GetMouseWorldPosition());
            // On indique que le drag est actif.
            _isDragging = true;
        }

        // Si le clic droit vient d'être relaché, on arrête le drag.
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            // Le joueur ne déplace plus la caméra.
            _isDragging = false;
        }

        // Si le joueur est en train de drag, on déplace la caméra.
        if (_isDragging)
        {
            // Position actuelle de la souris dans le monde.
            Vector3 currentPos = _cam.ScreenToWorldPoint(GetMouseWorldPosition());
            // Différence entre le point de depart et la position actuelle.
            Vector3 difference = _dragOrigin - currentPos;
            // On applique cette différence à la position de la caméra.
            transform.position += difference * DragSpeed;
        }
    }

    // Fonction qui gère le zoom de la caméra.
    private void UpdateZoom()
    {
        // On lit la valeur de la molette.
        float scroll = Mouse.current.scroll.ReadValue().y;

        // Si la molette a bougé, on change le zoom.
        if (scroll != 0)
        {
            // On zoome avec orthographicSize.
            _cam.orthographicSize -= scroll * ZoomSpeed * Time.deltaTime;
            // On empêche de dépasser les valeurs min et max avec Clamp .
            _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, MinZoom, MaxZoom);
        }
    }

    // Fonction qui convertit la position de la souris pour pouvoir l'utiliser dans le monde.
    private Vector3 GetMouseWorldPosition()
    {
        // Position de la souris sur l'écran.
        Vector3 mousePos = Mouse.current.position.ReadValue();
        // z correspond a la distance entre la camera et le plan du jeu.
        mousePos.z = Mathf.Abs(_cam.transform.position.z);
        // On retourne une position écran complète pour ScreenToWorldPoint.
        return mousePos;
    }

    // Fonction qui bloque la caméra dans les limites configurées.
    private void ClampPosition()
    {
        // On récupère la position actuelle.
        Vector3 pos = transform.position;

        // On bloque x entre LimitX.x et LimitX.y avec Clamp.
        pos.x = Mathf.Clamp(pos.x, LimitX.x, LimitX.y);
        // On bloque y entre LimitY.x et LimitY.y avec Clamp .
        pos.y = Mathf.Clamp(pos.y, LimitY.x, LimitY.y);

        // On applique la position corrigée.
        transform.position = pos;
    }
}
