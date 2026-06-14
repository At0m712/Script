using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Collections; 
using UnityEngine.Networking; 
using UnityEngine.Localization.Settings;

public enum TypeActionQuete
{
    TuerEnnemis,
    RamasserPieces,
    FaireTirs
}

[System.Serializable]
public struct Quete
{
    [Tooltip("Mets la CLÉ de traduction ici. Ex: QUETE_ENNEMIS")]
    public string titre; 
    public TypeActionQuete actionDemandee;
    
    [Header("Génération Aléatoire")]
    public int[] objectifsPossibles; 
    public int recompenseMin; 
    public int recompenseMax; 
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("La Banque de Quêtes")]
    public Quete[] banqueDeQuetes;

    [Header("Le Widget (Menu Principal)")]
    public TMP_Text texteApercuWidget; 

    [Header("La Popup Flottante")]
    public GameObject popupFlottante; 
    public GameObject contenuQueteActive;   
    public GameObject contenuQueteTerminee; 
    
    public TMP_Text texteTitreQuete;
    public TMP_Text texteProgression;
    public Slider jaugeProgression;
    public Button boutonRecuperer;
    public TMP_Text texteCompteARebours;

    private Quete queteDuJour;
    private float timerChrono = 0f;

    // Décalage horaire avec Google
    public TimeSpan differenceHeureInternet = TimeSpan.Zero;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        StartCoroutine(VerifierNouvelleJourneeInternet());
    }

    private IEnumerator VerifierNouvelleJourneeInternet()
    {
        UnityWebRequest request = new UnityWebRequest("https://google.com");
        request.downloadHandler = new DownloadHandlerBuffer();
        yield return request.SendWebRequest();

        DateTime dateActuelle = DateTime.Now; 

        if (request.result == UnityWebRequest.Result.Success)
        {
            string dateInternet = request.GetResponseHeader("date");
            if (!string.IsNullOrEmpty(dateInternet))
            {
                if (DateTime.TryParse(dateInternet, out dateActuelle))
                {
                    differenceHeureInternet = dateActuelle - DateTime.Now;
                }
            }
        }

        string dateAujourdhui = dateActuelle.ToString("yyyy-MM-dd");

        if (SaveManager.instance.data.dateQuete != dateAujourdhui)
        {
            SaveManager.instance.data.dateQuete = dateAujourdhui;
            
            int indexAleatoire = UnityEngine.Random.Range(0, banqueDeQuetes.Length);
            SaveManager.instance.data.indexQueteJour = indexAleatoire;
            Quete nouvelleQuete = banqueDeQuetes[indexAleatoire];
            
            if (nouvelleQuete.objectifsPossibles.Length > 0)
            {
                int indexObjectif = UnityEngine.Random.Range(0, nouvelleQuete.objectifsPossibles.Length);
                SaveManager.instance.data.objectifQueteJour = nouvelleQuete.objectifsPossibles[indexObjectif];
            }

            int paliers = (nouvelleQuete.recompenseMax - nouvelleQuete.recompenseMin) / 5;
            SaveManager.instance.data.recompenseQueteJour = nouvelleQuete.recompenseMin + (UnityEngine.Random.Range(0, paliers + 1) * 5);

            SaveManager.instance.data.progressionQuete = 0;
            SaveManager.instance.data.recompenseRecuperee = false;
            
            SaveManager.instance.SauvegarderPartie();
        }

        queteDuJour = banqueDeQuetes[SaveManager.instance.data.indexQueteJour];
        
        MettreAJourUI(); 
    }

    void Update()
    {
        if (SaveManager.instance.data.recompenseRecuperee && popupFlottante != null && popupFlottante.activeSelf)
        {
            timerChrono += Time.unscaledDeltaTime;
            if (timerChrono >= 1f)
            {
                CalculerTempsRestant();
                timerChrono = 0f; 
            }
        }

#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                SaveManager.instance.data.dateQuete = "1999-01-01";
                SaveManager.instance.SauvegarderPartie();
                StartCoroutine(VerifierNouvelleJourneeInternet());
                Debug.Log("🛠️ TRICHE ACTIVÉE : Nouvelle quête générée !");
            }
        }
#endif
    }

    public void AjouterProgression(TypeActionQuete actionFaite, int montant = 1)
    {
        if (SaveManager.instance.data.recompenseRecuperee) return;
        
        int objectif = SaveManager.instance.data.objectifQueteJour;
        if (SaveManager.instance.data.progressionQuete >= objectif) return;

        if (queteDuJour.actionDemandee == actionFaite)
        {
            SaveManager.instance.data.progressionQuete += montant;
            
            if (SaveManager.instance.data.progressionQuete > objectif)
                SaveManager.instance.data.progressionQuete = objectif;

            SaveManager.instance.SauvegarderPartie();
            MettreAJourUI();
        }
    }

    public void BoutonRecupererRecompense()
    {
        int objectif = SaveManager.instance.data.objectifQueteJour;
        if (SaveManager.instance.data.progressionQuete >= objectif && !SaveManager.instance.data.recompenseRecuperee)
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.AjouterArgent(SaveManager.instance.data.recompenseQueteJour);
            }
            
            if (ThemeManager.instance != null) 
            {
                ThemeManager.instance.RafraichirAffichageArgent(); 
            }
            
            SaveManager.instance.data.recompenseRecuperee = true;
            SaveManager.instance.SauvegarderPartie();
             
            MettreAJourUI();
        }
    }

    // Passé en public pour être appelé depuis le bouton !
    public void MettreAJourUI()
    {
        bool estTerminee = SaveManager.instance.data.recompenseRecuperee;
        int objectif = SaveManager.instance.data.objectifQueteJour;

        if (texteApercuWidget != null)
        {
            if (estTerminee) 
            {
                string texteFini = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "QUETE_WIDGET_FINI");
                texteApercuWidget.SetText(texteFini);
            }
            else 
            {
                texteApercuWidget.SetText("{0}/{1}", SaveManager.instance.data.progressionQuete, objectif);
            }
        }

        if (contenuQueteActive != null && contenuQueteTerminee != null)
        {
            contenuQueteActive.SetActive(!estTerminee);
            contenuQueteTerminee.SetActive(estTerminee);

            if (!estTerminee)
            {
                if(texteTitreQuete != null) 
                {
                    string formatTitre = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", queteDuJour.titre);
                    string titreFormatte = string.Format(formatTitre, objectif);

                    string formatRecompense = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "QUETE_RECOMPENSE");
                    string recompenseFormattee = string.Format(formatRecompense, SaveManager.instance.data.recompenseQueteJour);

                    texteTitreQuete.SetText(titreFormatte + recompenseFormattee);
                }
                
                if(texteProgression != null) 
                {
                    if (SaveManager.instance.data.progressionQuete >= objectif)
                    {
                        string texteQueteFinie = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "QUETE_FINIE");
                        texteProgression.SetText(texteQueteFinie);
                    }
                    else
                    {
                        texteProgression.SetText("{0} / {1}", SaveManager.instance.data.progressionQuete, objectif);
                    }
                }
                
                if (jaugeProgression != null)
                {
                    jaugeProgression.maxValue = objectif;
                    jaugeProgression.value = SaveManager.instance.data.progressionQuete;
                }

                if(boutonRecuperer != null) boutonRecuperer.interactable = (SaveManager.instance.data.progressionQuete >= objectif);
            }
            else
            {
                CalculerTempsRestant();
            }
        }
    }

    private void CalculerTempsRestant()
    {
        if (texteCompteARebours == null) return;
        
        DateTime maintenant = DateTime.Now + differenceHeureInternet;
        DateTime minuit = maintenant.Date.AddDays(1); 
        TimeSpan tempsRestant = minuit - maintenant;
        
        string texteFinal = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "QUETE_TITRE");
        string motEt = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "MOT_ET");

        if (tempsRestant.Hours > 0)
        {
            string cleHeure = tempsRestant.Hours > 1 ? "TEMPS_HEURES" : "TEMPS_HEURE";
            string cleMinute = tempsRestant.Minutes > 1 ? "TEMPS_MINUTES" : "TEMPS_MINUTE";
            
            string motHeure = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cleHeure);
            string motMinute = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cleMinute);
            
            texteFinal += tempsRestant.Hours + " " + motHeure + " " + motEt + " " + tempsRestant.Minutes + " " + motMinute;
        }
        else
        {
            string cleMinute = tempsRestant.Minutes > 1 ? "TEMPS_MINUTES" : "TEMPS_MINUTE";
            string cleSeconde = tempsRestant.Seconds > 1 ? "TEMPS_SECONDES" : "TEMPS_SECONDE";
            
            string motMinute = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cleMinute);
            string motSeconde = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cleSeconde);
            
            texteFinal += tempsRestant.Minutes + " " + motMinute + " " + motEt + " " + tempsRestant.Seconds + " " + motSeconde;
        }

        texteCompteARebours.text = texteFinal;
    }
}