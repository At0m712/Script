using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Settings; // NOUVEAU : Requis pour la traduction Unity

public class MultiplicateurUI : MonoBehaviour
{
    [Header("Textes à traduire")]
    public TextMeshProUGUI titreText;
    public TextMeshProUGUI boutonText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Chrono et Bouton")]
    public TextMeshProUGUI timerText;
    public Button boutonPub;

    [Header("Barre de progression")]
    public Image fond2X;
    public Image fond3X;
    public Image fond4X;
    public Color couleurInactif = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gris transparent
    public Color couleurActif = new Color(0.2f, 0.8f, 0.2f, 1f); // Vert

    void Update()
    {
        // On met à jour l'affichage en boucle pour le chronomètre
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

        // Calcul du temps restant
        if (!string.IsNullOrEmpty(SaveManager.instance.data.dateFinMultiplicateur) && DateTime.TryParse(SaveManager.instance.data.dateFinMultiplicateur, out finBonus))
        {
            if (finBonus > maintenant)
            {
                isActif = true;
                tempsRestant = finBonus - maintenant;
            }
            else
            {
                multi = 1; // Le temps est écoulé
            }
        }

        // 1. Mise à jour du chronomètre
        if (isActif)
        {
            // Format 00:00:00
            timerText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", tempsRestant.Hours, tempsRestant.Minutes, tempsRestant.Seconds);
        }
        else
        {
            timerText.text = "00:00:00";
        }

        // 2. Mise à jour des couleurs de la barre de progression
        fond2X.color = (multi >= 2) ? couleurActif : couleurInactif;
        fond3X.color = (multi >= 3) ? couleurActif : couleurInactif;
        fond4X.color = (multi >= 4) ? couleurActif : couleurInactif;

        // 3. Mise à jour des Textes (avec le système de traduction Unity)
        if (multi == 1) // Désactivé
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_DESACTIVE");
            boutonText.text = ObtenirTraduction("MULTI_BTN_2X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_2X");
            boutonPub.interactable = true;
        }
        else if (multi == 2) // Niveau 2X actif
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            boutonText.text = ObtenirTraduction("MULTI_BTN_3X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_3X");
            boutonPub.interactable = true;
        }
        else if (multi == 3) // Niveau 3X actif
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            boutonText.text = ObtenirTraduction("MULTI_BTN_4X");
            descriptionText.text = ObtenirTraduction("MULTI_DESC_4X");
            boutonPub.interactable = true;
        }
        else if (multi == 4) // Niveau 4X (Max) actif
        {
            titreText.text = ObtenirTraduction("MULTI_TITRE_ACTIF");
            
            // Si le joueur a déjà 1 heure de réserve (on vérifie à 59min 55sec pour éviter les bugs d'arrondi)
            if (tempsRestant.TotalMinutes >= 59.9f)
            {
                boutonText.text = ObtenirTraduction("MULTI_BTN_MAX");
                descriptionText.text = ObtenirTraduction("MULTI_DESC_MAX");
                boutonPub.interactable = false; // On grise le bouton, il ne peut plus regarder de pub
            }
            else
            {
                boutonText.text = ObtenirTraduction("MULTI_BTN_TEMPS");
                descriptionText.text = ObtenirTraduction("MULTI_DESC_TEMPS");
                boutonPub.interactable = true;
            }
        }
    }

    // CORRECTION ICI : Utilisation de la base de données de texte Unity
    string ObtenirTraduction(string cle)
    {
        // "TexteUI" correspond au nom de ta table (String Table) dans Unity Localization
        return LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cle);
    }
}