using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI; // 👉 NOUVEAU : Requis pour modifier les Images
using System.Collections;

public class PopupModeManager : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Interface")]
    public RectTransform popupRect; 
    public CanvasGroup fondSombre; 
    
    [Header("Affichage du Mode Choisi")]
    [Tooltip("L'image sur ton bouton principal qui va changer")]
    public Image imageAffichageMode; 
    [Tooltip("Le visuel pour le mode Classique")]
    public Sprite iconeClassique; 
    [Tooltip("Le visuel pour le mode Speedrun")]
    public Sprite iconeSpeedrun; 
    [Tooltip("Le visuel pour le mode 1v1")]
    public Sprite icone1v1; 

    [Header("Paramètres d'Animation")]
    public float positionOuverteY = 0f; 
    public float positionFermeeY = -2000f; 
    public float dureeOuverture = 0.45f; 
    public float dureeFermeture = 0.2f; 
    public float forceRebond = 3.5f;

    private Coroutine animationEnCours;
    private bool estOuvert = false;

    void Start()
    {
        if (popupRect == null) popupRect = GetComponent<RectTransform>();
        
        popupRect.anchoredPosition = new Vector2(popupRect.anchoredPosition.x, positionFermeeY);
        
        if (fondSombre != null)
        {
            fondSombre.alpha = 0f;
            fondSombre.blocksRaycasts = false;
        }

        ActualiserAffichage();
    }

    public void OuvrirPopup()
    {
        if (estOuvert) return;
        estOuvert = true;
        if (animationEnCours != null) StopCoroutine(animationEnCours);
        animationEnCours = StartCoroutine(AnimerPopup(positionOuverteY, 1f, dureeOuverture));
    }

    public void FermerPopup()
    {
        if (!estOuvert) return;
        estOuvert = false;
        if (animationEnCours != null) StopCoroutine(animationEnCours);
        animationEnCours = StartCoroutine(AnimerPopup(positionFermeeY, 0f, dureeFermeture));
    }

    private IEnumerator AnimerPopup(float cibleY, float cibleAlphaFond, float dureeActuelle)
    {
        float tempsEcoule = 0f;
        float departY = popupRect.anchoredPosition.y;
        float departAlpha = fondSombre != null ? fondSombre.alpha : 0f;

        if (fondSombre != null && cibleAlphaFond > 0f) fondSombre.blocksRaycasts = true;

        while (tempsEcoule < dureeActuelle)
        {
            tempsEcoule += Time.unscaledDeltaTime; 
            float t = tempsEcoule / dureeActuelle;
            
            float progressionY = 0f;

            if (cibleY == positionOuverteY) 
            {
                float c3 = forceRebond + 1f;
                progressionY = 1f + c3 * Mathf.Pow(t - 1f, 3f) + forceRebond * Mathf.Pow(t - 1f, 2f);
            }
            else 
            {
                progressionY = t * t * t; 
            }

            popupRect.anchoredPosition = new Vector2(popupRect.anchoredPosition.x, Mathf.LerpUnclamped(departY, cibleY, progressionY));
            
            if (fondSombre != null) fondSombre.alpha = Mathf.Lerp(departAlpha, cibleAlphaFond, t);

            yield return null;
        }

        popupRect.anchoredPosition = new Vector2(popupRect.anchoredPosition.x, cibleY);
        
        if (fondSombre != null)
        {
            fondSombre.alpha = cibleAlphaFond;
            if (cibleAlphaFond == 0f) fondSombre.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.delta.y < 0 || popupRect.anchoredPosition.y < positionOuverteY)
        {
            float nouvellePosition = popupRect.anchoredPosition.y + eventData.delta.y;
            if (nouvellePosition > positionOuverteY) nouvellePosition = positionOuverteY;
            popupRect.anchoredPosition = new Vector2(popupRect.anchoredPosition.x, nouvellePosition);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (popupRect.anchoredPosition.y < positionOuverteY - 150f || eventData.delta.y < -10f)
            FermerPopup();
        else
            OuvrirPopup();
    }

    public void ChoisirClassique()
    {
        PlayerPrefs.SetString("ModeChoisi", "Normal");
        PlayerPrefs.Save();
        ActualiserAffichage();
        FermerPopup(); 
    }

    public void ChoisirSpeedrun()
    {
        PlayerPrefs.SetString("ModeChoisi", "Speedrun");
        PlayerPrefs.Save();
        ActualiserAffichage();
        FermerPopup();
    }

    public void Choisir1v1()
    {
        PlayerPrefs.SetString("ModeChoisi", "1v1");
        PlayerPrefs.Save();
        ActualiserAffichage();
        FermerPopup();
    }

    // 👉 NOUVEAU : La fonction qui gère le changement d'image
    private void ActualiserAffichage()
    {
        if (imageAffichageMode != null)
        {
            string mode = PlayerPrefs.GetString("ModeChoisi", "Normal");
            
            if (mode == "Normal") 
                imageAffichageMode.sprite = iconeClassique;
            else if (mode == "Speedrun") 
                imageAffichageMode.sprite = iconeSpeedrun;
            else if (mode == "1v1") 
                imageAffichageMode.sprite = icone1v1;
        }
    }
}