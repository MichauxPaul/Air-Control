using UnityEngine;
using UnityEngine.InputSystem;

public class CamFollowMouse : MonoBehaviour
{
    [Header("Drag")]
    //Vitesse à laquelle la caméra bouge
    public float dragSpeed = 1f;

    [Header("Zoom")]
    //Réglages du zoom
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 20f;

    [Header("Limits")]
    //Limites du déplacement de la caméra
    public Vector2 limitX = new Vector2(-50, 50);
    public Vector2 limitY = new Vector2(-50, 50);

    private Vector3 dragOrigin;
    private bool isDragging;

    private Camera cam;

    void Start()
    {
        //On récupère la caméra principale
        cam = Camera.main;
    }

    void Update()
    {
        //On gère le déplacement
        HandleDrag();
        //On gère le zoom
        HandleZoom();
        //On bloque la caméra dans les limites
        ClampPosition();
    }

    //fonction appeller quand on appuie sur le clic droit
    void HandleDrag()
    {
        //On mémorise où on a cliqué dans le monde
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            dragOrigin = cam.ScreenToWorldPoint(GetMouseWorldPosition());
            isDragging = true;
        }
        //Quand on relâche
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        //pendant que l'on déplace la caméra
        if (isDragging)
        {
            //On regarde où est la souris
            Vector3 currentPos = cam.ScreenToWorldPoint(GetMouseWorldPosition());
            //On compare avec le point de départ
            Vector3 difference = dragOrigin - currentPos;
            //On déplace la caméra de la différence
            transform.position += difference * dragSpeed;
        }
    }

    //fonction appeller lors du zoom
    void HandleZoom()
    {
        //On lit la molette
        float scroll = Mouse.current.scroll.ReadValue().y;

        
        if (scroll != 0)
        {
            //On change le zoom
            cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            //On empêche de trop zoomer et de trop dézoomer
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    //conversion entre la position écrant et la position dans le monde
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = Mathf.Abs(cam.transform.position.z); 
        return mousePos;
    }

    //Limites de la caméra
    void ClampPosition()
    {
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, limitX.x, limitX.y);
        pos.y = Mathf.Clamp(pos.y, limitY.x, limitY.y);

        transform.position = pos;
    }
}