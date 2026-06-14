using UnityEngine;
using UnityEngine.EventSystems;

public class ControleurCameraCourbe : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Lien avec la Caméra")]
    public CameraFollow scriptCamera; 
    public float limiteAngleCamera = 90f; 

    [Header("L'Interface UI")]
    public RectTransform centreRoueUI; 
    public float angleMaxArc = 60f; 
    
    // --- NOUVEAU : Amortisseur UI ---
    [Header("Fluidité Visuelle")]
    public float fluiditeCurseur = 25f; // Plus c'est élevé, plus le rond colle au doigt vite
    
    private float angleCibleUI = 0f;
    private float angleVisuelActuel = 0f;

    private Canvas parentCanvas;

    void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        CalculerCible(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        CalculerCible(eventData);
    }

    private void CalculerCible(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            centreRoueUI, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        // On calcule l'angle (Rappel : avec le "-" sur le Y car ta courbe est un "sourire")
        float angle = Mathf.Atan2(localPoint.x, -localPoint.y) * Mathf.Rad2Deg;

        // On définit la CIBLE mathématique (sans déplacer le rond tout de suite)
        angleCibleUI = Mathf.Clamp(angle, -angleMaxArc, angleMaxArc);

        // On envoie immédiatement l'ordre à la caméra pour qu'elle n'ait pas de retard
        if (scriptCamera != null)
        {
            float pourcentage = angleCibleUI / angleMaxArc;
            scriptCamera.angleRotation = pourcentage * limiteAngleCamera;
        }
    }

    // --- NOUVEAU : La mise à jour visuelle fluide ---
    void Update()
    {
        // On lisse le mouvement du rond UI vers la cible
        angleVisuelActuel = Mathf.Lerp(angleVisuelActuel, angleCibleUI, Time.deltaTime * fluiditeCurseur);
        
        // On applique la rotation lissée au pivot
        centreRoueUI.localRotation = Quaternion.Euler(0, 0, angleVisuelActuel);
    }
}