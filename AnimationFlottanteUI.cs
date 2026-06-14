using UnityEngine;

public class AnimationFlottanteUI : MonoBehaviour
{
    [Header("Paramètres de Mouvement")]
    [Tooltip("De combien de pixels la flèche monte et descend")]
    public float amplitude = 5f; 
    
    [Tooltip("La vitesse du va-et-vient")]
    public float vitesse = 4f;   

    private RectTransform rectTransform;
    private Vector2 positionInitiale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // On mémorise la position d'origine du RectTransform
        positionInitiale = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // 1. On calcule l'oscillation grâce au sinus (ignore la pause du jeu avec unscaledTime)
        float vagueSinus = Mathf.Sin(Time.unscaledTime * vitesse);

        // 2. On applique le mouvement de haut en bas par rapport à la position de départ
        float decalageY = vagueSinus * amplitude;
        rectTransform.anchoredPosition = new Vector2(positionInitiale.x, positionInitiale.y + decalageY);
    }
}