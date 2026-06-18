using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Settings; 

public class MultiplicateurUI : MonoBehaviour
{
    [Header("Textes Principaux")]
    public TextMeshProUGUI titreText;
    public TextMeshProUGUI boutonText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI texteStatut; 

    [Header("Chrono et Bouton")]
    public TextMeshProUGUI timerText;
    public Button boutonPub;

    [Header("Barre de Temps (Rouge)")]
    public Image barreTempsRouge; 

    [Header("Barre de progression Multiplicateur")]
    public Image fond2X;
    public Image fond3X;
    public Image fond4X;
    public Color couleurInactif = new Color(0.5f, 0.5f, 0.5f, 0.8f); 
    public Color couleurActif = new Color(0.2f, 0.8f, 0.2f, 1f); 

    [Header("Personnages (Objets à allumer/éteindre)")]
    public GameObject perso1X;
    public GameObject perso2X;
    public GameObject perso3X;
    public GameObject perso4X;

    [Header("Fonds Statut (Objets à allumer/éteindre)")]
    public GameObject fondStatut1X;
    public GameObject fondStatut2X;
    public GameObject fondStatut3X;
    public GameObject fondStatut4X;

    [Header("Élément Extra (S'allume à partir de 2X)")]
    public GameObject elementExtra; 

    // NOUVEAU : Gestion de la vidéo et du point d'exclamation
    [Header("Vidéo et Notifications")]
    [Tooltip("L'objet contenant la vidéo (S'allume de 2X à 4X)")]
    public GameObject objetVideo; 
    [Tooltip("L'image du point d'exclamation (S'allume uniquement en 1X)")]
    public GameObject imageExclamation; 

    [Header("Image dynamique (Changement de couleur)")]
    public Image imageDynamiqueCouleur;
    public Color couleurImage1X = new Color(0.5f, 0.5f, 0.5f, 1f); 
    public Color couleurImage2X = new Color(0.2f, 0.8f, 0.2f, 1f); 
    public Color couleurImage3X = new Color(0.1f, 0.5f, 0.8f, 1f); 
    public Color couleurImage4X = new Color(1f, 0.8f, 0f, 1f);     

    private const float TEMPS_MAX_SECONDES = 3600f; 

    void Update()
    {
        MettreAJourAffichage();
    }

    public void MettreAJourAffichage()
    {
        if (SaveManager.instance == null) return;

        int multi = SaveManager.instance.data.multiplicateurArgentActuel;
        DateTime finBonus;
        DateTime maintenant = DateTime.Now;
        
        if (QuestManager.instance != null) 
            maintenant += QuestManager.instance.differenceHeureInternet;

        bool isActif = false;
        TimeSpan tempsRestant = TimeSpan.Zero;

        if (!string.IsNullOrEmpty(SaveManager.instance.data.dateFinMultiplicateur) && DateTime.TryParse(SaveManager.instance.data.dateFinMultiplicateur, out finBonus))
        {
            if (finBonus > maintenant)
            {
                isActif = true;
                tempsRestant = finBonus - maintenant;
            }
            else
            {
                multi = 1; 
            }
        }

        if (isActif)
        {
            timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", tempsRestant.Hours, tempsRestant.Minutes, tempsRestant.Seconds);
            
            if (barreTempsRouge != null)
            {
                float ratioTemps = (float)tempsRestant.TotalSeconds / TEMPS_MAX_SECONDES;
                barreTempsRouge.fillAmount = Mathf.Clamp01(ratioTemps); 
            }
        }
        else
        {
            timerText.text = "00:00:00";
            
            if (barreTempsRouge != null)
            {
                barreTempsRouge.fillAmount = 0f;
            }
        }

        fond2X.color = (multi >= 2) ? couleurActif : couleurInactif;
        fond3X.color = (multi >= 3) ? couleurActif : couleurInactif;
        fond4X.color = (multi >= 4) ? couleurActif : couleurInactif;

        if (multi == 1) 
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_DESACTIVE");
            boutonText.text = ObtenirTraduction("MULTI_BTN_2X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_2X");
            texteStatut.text = ObtenirTraduction("MULTI_STATUT_1X");

            ActiverVisuels(true, false, false, false); 
            
            if (imageDynamiqueCouleur != null) imageDynamiqueCouleur.color = couleurImage1X;

            boutonPub.interactable = true;
        }
        else if (multi == 2) 
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            boutonText.text = ObtenirTraduction("MULTI_BTN_3X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_3X");
            texteStatut.text = ObtenirTraduction("MULTI_STATUT_2X");

            ActiverVisuels(false, true, false, false); 
            
            if (imageDynamiqueCouleur != null) imageDynamiqueCouleur.color = couleurImage2X;

            boutonPub.interactable = true;
        }
        else if (multi == 3) 
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            boutonText.text = ObtenirTraduction("MULTI_BTN_4X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_4X");
            texteStatut.text = ObtenirTraduction("MULTI_STATUT_3X");

            ActiverVisuels(false, false, true, false); 
            
            if (imageDynamiqueCouleur != null) imageDynamiqueCouleur.color = couleurImage3X;

            boutonPub.interactable = true;
        }
        else if (multi == 4) 
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            texteStatut.text = ObtenirTraduction("MULTI_STATUT_4X");

            ActiverVisuels(false, false, false, true); 
            
            if (imageDynamiqueCouleur != null) imageDynamiqueCouleur.color = couleurImage4X;
            
            if (tempsRestant.TotalMinutes >= 59.9f)
            {
                boutonText.text = ObtenirTraduction("MULTI_BTN_MAX");
                descriptionText.text = ObtenirTraduction("MULTI_DESC_MAX");
                boutonPub.interactable = false;
            }
            else
            {
                boutonText.text = ObtenirTraduction("MULTI_BTN_TEMPS");
                descriptionText.text = ObtenirTraduction("MULTI_DESC_TEMPS");
                boutonPub.interactable = true;
            }
        }
    }

    private void ActiverVisuels(bool etat1X, bool etat2X, bool etat3X, bool etat4X)
    {
        if (perso1X != null) perso1X.SetActive(etat1X);
        if (perso2X != null) perso2X.SetActive(etat2X);
        if (perso3X != null) perso3X.SetActive(etat3X);
        if (perso4X != null) perso4X.SetActive(etat4X);

        if (fondStatut1X != null) fondStatut1X.SetActive(etat1X);
        if (fondStatut2X != null) fondStatut2X.SetActive(etat2X);
        if (fondStatut3X != null) fondStatut3X.SetActive(etat3X);
        if (fondStatut4X != null) fondStatut4X.SetActive(etat4X);

        if (elementExtra != null) elementExtra.SetActive(etat2X || etat3X || etat4X);

        // MISE À JOUR : Gestion de la Vidéo et de l'Exclamation
        // Le point d'exclamation s'allume UNIQUEMENT si on est en 1X (etat1X est vrai)
        if (imageExclamation != null) imageExclamation.SetActive(etat1X);
        
        // La vidéo s'allume si on est en 2X, 3X ou 4X
        if (objetVideo != null) objetVideo.SetActive(etat2X || etat3X || etat4X);
    }

    string ObtenirTraduction(string cle)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cle);
    }
}