using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem; 
using TMPro; 
using UnityEngine.Localization.Settings; 

public enum TypeCadeau { Pieces, Skin }

[Serializable]
public struct Cadeau
{
    public TypeCadeau type;
    public int montantPieces; 
    public int indexDuSkin; 
    public string texteAffichage; 
}

public class CalendrierCadeaux : MonoBehaviour
{
    [Header("Paramètres du Calendrier")]
    public int heureDeReset = 8; 
    public Cadeau[] recompenses = new Cadeau[8]; 
    
    [Header("Paramètres de Traduction")]
    public string tableLocalization = "TexteUI"; 
    public string cleLocalizationJour = "CALENDRIER_JOUR"; 

    [Header("Interface UI")]
    public GameObject fenetrePopup;
    public Image[] casesJours; 
    
    [Tooltip("Le grand texte de titre qui affichera 'Jour 1', 'Day 2', etc.")]
    public TMP_Text texteTitreGeneral; // <--- LE NOUVEAU TEXTE UNIQUE EST ICI

    [Header("Le Contour Jaune Unique")]
    public GameObject contourJauneUnique; 
    
    [Range(1f, 1.5f)] 
    public float multiplicateurTaille = 1.15f; 

    [Header("Couleurs des cases et des textes")]
    public Color couleurNormal = Color.white;
    public Color couleurAcquis = new Color(0.5f, 0.5f, 0.5f, 0.6f); 
    public Color couleurVerrouille = new Color(0.7f, 0.7f, 0.7f, 1f);

    private string cleDate = "DateDernierCalendrier";
    private string cleJour = "JourActuelCalendrier";
    private int jourActuel;
    private int joursSimules = 0;

    void Start()
    {
        jourActuel = PlayerPrefs.GetInt(cleJour, 0);
        VerifierCalendrier();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            TricherJourSuivant();
        }
    }

    void TricherJourSuivant()
    {
        joursSimules++; 
        Debug.LogWarning($"🕒 TRICHE : Temps avancé de {joursSimules} jour(s).");
        VerifierCalendrier();
    }

    DateTime ObtenirDateActuelle()
    {
        return DateTime.Now.AddDays(joursSimules);
    }

    public void VerifierCalendrier()
    {
        bool peutReclamer = false;

        if (PlayerPrefs.HasKey(cleDate))
        {
            string dateString = PlayerPrefs.GetString(cleDate);
            DateTime derniereDate = DateTime.Parse(dateString);
            DateTime dernierReset = ObtenirDernierReset();

            if (derniereDate < dernierReset) peutReclamer = true;
        }
        else
        {
            peutReclamer = true; 
        }

        if (peutReclamer)
        {
            MettreAJourVisuelCases();
            fenetrePopup.SetActive(true); 
        }
        else
        {
            fenetrePopup.SetActive(false); 
        }
    }

    DateTime ObtenirDernierReset()
    {
        DateTime maintenant = ObtenirDateActuelle();
        DateTime resetAujourdhui = new DateTime(maintenant.Year, maintenant.Month, maintenant.Day, heureDeReset, 0, 0);

        if (maintenant >= resetAujourdhui) return resetAujourdhui;
        else return resetAujourdhui.AddDays(-1);
    }

    void MettreAJourVisuelCases()
    {
        // 🌍 On met à jour LE TITRE GÉNÉRAL en haut de la fenêtre
        string formatJourTraduit = LocalizationSettings.StringDatabase.GetLocalizedString(tableLocalization, cleLocalizationJour);
        
        if (texteTitreGeneral != null && !string.IsNullOrEmpty(formatJourTraduit))
        {
            // On remplace le {0} par le numéro du jour actuel (jourActuel commence à 0, donc on fait + 1)
            texteTitreGeneral.text = string.Format(formatJourTraduit, jourActuel + 1);
        }

        // --- GESTION DES 8 CASES ---
        for (int i = 0; i < casesJours.Length; i++)
        {
            casesJours[i].transform.localScale = Vector3.one; 

            Transform coche = casesJours[i].transform.Find("CocheVerte");
            
            TMP_Text compTexteMontant = null;
            Transform objetTexteMontant = casesJours[i].transform.Find("TexteMontant");
            if (objetTexteMontant != null)
            {
                compTexteMontant = objetTexteMontant.GetComponent<TMP_Text>();
                if (compTexteMontant != null) compTexteMontant.text = recompenses[i].texteAffichage;
            }

            Color couleurAAppliquer = couleurNormal;

            if (i < jourActuel)
            {
                couleurAAppliquer = couleurAcquis;
                if (coche != null) coche.gameObject.SetActive(true); 
            }
            else if (i == jourActuel)
            {
                couleurAAppliquer = couleurNormal;
                casesJours[i].transform.localScale = new Vector3(multiplicateurTaille, multiplicateurTaille, 1f);
                if (coche != null) coche.gameObject.SetActive(false);

                if (contourJauneUnique != null)
                {
                    contourJauneUnique.transform.SetParent(casesJours[i].transform); 
                    contourJauneUnique.transform.localPosition = Vector3.zero;       
                    contourJauneUnique.transform.localScale = Vector3.one;          
                    contourJauneUnique.SetActive(true);                              
                }
            }
            else
            {
                couleurAAppliquer = couleurVerrouille;
                if (coche != null) coche.gameObject.SetActive(false);
            }

            // Application de la couleur sur le fond de la case ET sur le texte du montant (+50)
            casesJours[i].color = couleurAAppliquer; 
            if (compTexteMontant != null) compTexteMontant.color = couleurAAppliquer; 
        }
    }

public void ReclamerCadeau()
    {
        Cadeau cadeauDuJour = recompenses[jourActuel];

        // --- 1. DISTRIBUTION DE LA RÉCOMPENSE ---
        if (cadeauDuJour.type == TypeCadeau.Pieces)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AjouterArgent(cadeauDuJour.montantPieces);
            }
            else
            {
                SaveManager.instance.data.argentTotal += cadeauDuJour.montantPieces;
                SaveManager.instance.SauvegarderPartie();
            }
        }
        else if (cadeauDuJour.type == TypeCadeau.Skin)
        {
            if (!SaveManager.instance.data.skinsDebloques.Contains(cadeauDuJour.indexDuSkin))
            {
                SaveManager.instance.data.skinsDebloques.Add(cadeauDuJour.indexDuSkin);
                SaveManager.instance.SauvegarderPartie();
                Debug.Log("Skin n°" + cadeauDuJour.indexDuSkin + " débloqué via le calendrier !");
            }
        }

        // --- 2. MISE À JOUR DE TOUTE L'INTERFACE BOUTIQUE ---
        // Placée ici, elle s'active qu'on ait gagné des pièces OU un skin !
        if (ThemeManager.instance != null) 
        {
            ThemeManager.instance.MettreAJourArgentUI();
            ThemeManager.instance.MettreAJourBoutonsBoutique(); 
        }

        // --- 3. SAUVEGARDE DU TEMPS ET FERMETURE ---
        PlayerPrefs.SetString(cleDate, ObtenirDateActuelle().ToString());
        jourActuel++;
        
        if (jourActuel >= casesJours.Length) 
        {
            jourActuel = 0;
        }
        
        PlayerPrefs.SetInt(cleJour, jourActuel);
        fenetrePopup.SetActive(false);
    }
}