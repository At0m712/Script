using UnityEngine;
using UnityEngine.EventSystems; // Requis pour détecter le clic/touche tactile

// On force le script à utiliser les interfaces de clic d'Unity
public class BoutonEnfonce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    
    [Header("Paramètres d'animation")]
    [Tooltip("Décalage en pixels (X, Y) appliqué quand le bouton est pressé")]
    public Vector2 decalageCible = new Vector2(20f, -20f); 
    
    private bool estEnfonce = false;

    void Awake()
    {
        // On récupère le composant de positionnement du bouton
        rectTransform = GetComponent<RectTransform>();
    }

    // ⬇️ Appelé AUTOMATIQUEMENT à la milliseconde où le joueur appuie sur le bouton
    public void OnPointerDown(PointerEventData eventData)
    {
        if (estEnfonce) return;
        
        estEnfonce = true;
        rectTransform.anchoredPosition += decalageCible; // On décale le bouton
    }

    // ⬆️ Appelé AUTOMATIQUEMENT dès que le joueur relâche le bouton (ou retire son doigt)
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!estEnfonce) return;
        
        estEnfonce = false;
        rectTransform.anchoredPosition -= decalageCible; // On remet le bouton à sa place d'origine
    }
}