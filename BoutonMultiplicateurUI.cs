using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Settings;

public class BoutonMultiplicateurUI : MonoBehaviour
{
    [Header("Éléments du Bouton")]
    [Tooltip("Le texte qui affiche le chrono (ex: 00:10:00)")]
    public TextMeshProUGUI texteChrono; 
    
    [Tooltip("L'image de la jauge qui se remplit (Type: Filled)")]
    public Image jaugeTemps; 
    
    [Tooltip("Le texte qui affiche le multiplicateur actuel (ex: 2X)")]
    public TextMeshProUGUI texteNiveau; 

    [Header("Visuels d'état (Optionnel)")]
    [Tooltip("Objet à allumer quand c'est inactif (ex: icône grise)")]
    public GameObject visuelInactif; 
    [Tooltip("Objet à allumer quand c'est actif (ex: icône qui brille)")]
    public GameObject visuelActif; 

    private const float TEMPS_MAX_SECONDES = 3600f; // 1 Heure

    void Update()
    {
        // On actualise le bouton en permanence pour suivre le chrono
        SynchroniserBouton();
    }

    private void SynchroniserBouton()
    {
        if (SaveManager.instance == null) return;

        int multi = SaveManager.instance.data.multiplicateurArgentActuel;
        DateTime finBonus;
        DateTime maintenant = DateTime.Now;

        // Sécurité pour l'heure internet (anti-triche)
        if (QuestManager.instance != null) 
            maintenant += QuestManager.instance.differenceHeureInternet;

        bool isActif = false;
        TimeSpan tempsRestant = TimeSpan.Zero;

        // Vérification du temps restant
        if (!string.IsNullOrEmpty(SaveManager.instance.data.dateFinMultiplicateur) && DateTime.TryParse(SaveManager.instance.data.dateFinMultiplicateur, out finBonus))
        {
            if (finBonus > maintenant)
            {
                isActif = true;
                tempsRestant = finBonus - maintenant;
            }
            else
            {
                multi = 1; // Temps écoulé
            }
        }

        // 1. Synchronisation du Chrono
        if (texteChrono != null)
        {
            if (isActif)
            {
                texteChrono.text = string.Format("{0:D2}:{1:D2}:{2:D2}", tempsRestant.Hours, tempsRestant.Minutes, tempsRestant.Seconds);
            }
            else
            {
                // Quand c'est inactif, on peut afficher 00:00:00 ou un texte traduit comme "OFF"
                texteChrono.text = ObtenirTraduction("MULTI_STATUT_1X"); 
            }
        }

        // 2. Synchronisation de la Jauge de temps
        if (jaugeTemps != null)
        {
            if (isActif)
            {
                float ratioTemps = (float)tempsRestant.TotalSeconds / TEMPS_MAX_SECONDES;
                jaugeTemps.fillAmount = Mathf.Clamp01(ratioTemps);
            }
            else
            {
                jaugeTemps.fillAmount = 0f;
            }
        }

        // 3. Synchronisation du Texte du Multiplicateur (1X, 2X, etc.)
        if (texteNiveau != null)
        {
            if (multi > 1)
            {
                texteNiveau.text = multi + "X"; // Affiche "2X", "3X" ou "4X"
            }
            else
            {
                texteNiveau.text = "1X"; // Ou laisse vide selon ton design
            }
        }

        // 4. Gestion des visuels d'état
        if (visuelInactif != null) visuelInactif.SetActive(!isActif);
        if (visuelActif != null) visuelActif.SetActive(isActif);
    }

    // Réutilisation de ton système de langue pour le texte inactif
    string ObtenirTraduction(string cle)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cle);
    }
}