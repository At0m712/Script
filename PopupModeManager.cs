using UnityEngine;
using UnityEngine.EventSystems; 
using UnityEngine.UI; 
using System.Collections;
using TMPro; // 🚀 NOUVEAU : Requis pour modifier le texte du niveau

public class PopupModeManager : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [Header("Interface")]
    public RectTransform popupRect; 
    public CanvasGroup fondSombre; 
    
    [Header("Affichage du Mode Choisi")]
    public Image imageAffichageMode; 
    public Sprite iconeClassique; 
    public Sprite iconeSpeedrun; 
    public Sprite icone1v1; 

    // 🚀 NOUVEAU : Gestion des niveaux Speedrun
    [Header("Sélection Niveau Speedrun")]
    [Tooltip("Glissez ici le texte qui affiche le chiffre du niveau entre les deux flèches")]
    public TMP_Text texteNiveauSpeedrun; 
    private int indexSpeedrunChoisi = 0; // Va de 0 à 3

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

        // On charge le dernier niveau joué
        indexSpeedrunChoisi = PlayerPrefs.GetInt("NiveauSpeedrunActuel", 0);
        MettreAJourTexteSpeedrun();
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
            else progressionY = t * t * t; 

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
        if (popupRect.anchoredPosition.y < positionOuverteY - 150f || eventData.delta.y < -10f) FermerPopup();
        else OuvrirPopup();
    }

    // ==========================================
    // CHOIX DES MODES
    // ==========================================

    public void ChoisirClassique()
    {
        PlayerPrefs.SetString("ModeChoisi", "Normal");
        PlayerPrefs.Save();
        ActualiserAffichage();
        FermerPopup(); 
    }

    // 🚀 NOUVEAU : Quand on clique sur le gros bouton "Speedrun"
    public void ChoisirSpeedrun()
    {
        PlayerPrefs.SetString("ModeChoisi", "Speedrun");
        PlayerPrefs.SetInt("NiveauSpeedrunActuel", indexSpeedrunChoisi);
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

    // ==========================================
    // GESTION DES FLÈCHES SPEEDRUN
    // ==========================================

    // 🚀 NOUVEAU : Fonction appelée par vos deux boutons flèches
    public void ChangerNiveauSpeedrun(int direction)
    {
        indexSpeedrunChoisi += direction;
        
        // Boucle : Si on dépasse 3, on revient à 0. Si on va sous 0, on va à 3.
        if (indexSpeedrunChoisi > 3) indexSpeedrunChoisi = 0;
        if (indexSpeedrunChoisi < 0) indexSpeedrunChoisi = 3;
        
        MettreAJourTexteSpeedrun();

        // Optionnel : Quand on touche aux flèches, ça sélectionne automatiquement le mode Speedrun
        PlayerPrefs.SetString("ModeChoisi", "Speedrun");
        PlayerPrefs.SetInt("NiveauSpeedrunActuel", indexSpeedrunChoisi);
        PlayerPrefs.Save();
        ActualiserAffichage();
    }

    private void MettreAJourTexteSpeedrun()
    {
        if (texteNiveauSpeedrun != null)
        {
            // Le code utilise 0, 1, 2, 3, mais le joueur voit 1, 2, 3, 4
            texteNiveauSpeedrun.text = (indexSpeedrunChoisi + 1).ToString(); 
        }
    }

    private void ActualiserAffichage()
    {
        if (imageAffichageMode != null)
        {
            string mode = PlayerPrefs.GetString("ModeChoisi", "Normal");
            if (mode == "Normal") imageAffichageMode.sprite = iconeClassique;
            else if (mode == "Speedrun") imageAffichageMode.sprite = iconeSpeedrun;
            else if (mode == "1v1") imageAffichageMode.sprite = icone1v1;
        }
    }
}