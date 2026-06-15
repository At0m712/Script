using UnityEngine;
using TMPro;

public class ChronoManager : MonoBehaviour
{
    public static ChronoManager instance;

    [Header("Interface (UI)")]
    public TMP_Text texteChrono; // Le chrono qui défile en direct pendant la course

    [Header("Nettoyage de l'écran en Speedrun")]
    [Tooltip("Glisse ici l'UI des pièces, le texte du niveau, les jauges de power-up...")]
    public GameObject[] elementsAMasquer;
    
    [Header("Menu de Fin (Victoire)")]
    public GameObject panelVictoire; 
    
    public TMP_Text texteTempsFinal;    // Affichera le temps fait sur la run
    public TMP_Text texteMeilleurTemps; // Affichera le record absolu du joueur

    private float tempsEcoule = 0f;
    private bool chronoActif = false;
    private bool aDemarre = false;

    private Rigidbody joueurRb; 
    private float dernierTempsAffiche = -1f;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    void Start()
    {
        string modeDeJeu = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (texteChrono != null) texteChrono.gameObject.SetActive(true);

        if (modeDeJeu == "Speedrun")
        {
            foreach (GameObject element in elementsAMasquer)
            {
                if (element != null) element.SetActive(false);
            }
        }
        else
        {
            foreach (GameObject element in elementsAMasquer)
            {
                if (element != null) element.SetActive(true);
            }
        }

        if (panelVictoire != null) panelVictoire.SetActive(false); 
        
        // On utilise notre nouvelle fonction pour tout mettre à zéro proprement au départ
        ReinitialiserChrono();
    }

    void TrouverJoueur()
    {
        if (GameManager.instance != null && GameManager.instance.joueurRb != null)
        {
            joueurRb = GameManager.instance.joueurRb;
        }
    }

    void Update()
    {
        if (joueurRb == null && !aDemarre)
        {
            TrouverJoueur();
            return; 
        }

        // Si le chrono n'est pas encore actif et qu'on est en mode Normal/Speedrun, 
        // il démarre dès que la boule bouge.
        if (!aDemarre && joueurRb != null)
        {
            Vector3 vitesseHorizontale = new Vector3(joueurRb.linearVelocity.x, 0f, joueurRb.linearVelocity.z);

            if (vitesseHorizontale.magnitude > 0.1f)
            {
                DemarrerChrono();
            }
        }

        if (chronoActif)
        {
            tempsEcoule += Time.deltaTime;

            if (tempsEcoule - dernierTempsAffiche >= 0.1f)
            {
                MettreAJourAffichage();
                dernierTempsAffiche = tempsEcoule;
            }
        }
    }

    void MettreAJourAffichage()
    {
        if (texteChrono == null) return;

        if (tempsEcoule < 60f)
        {
            texteChrono.text = tempsEcoule.ToString("F1"); 
        }
        else
        {
            int minutes = Mathf.FloorToInt(tempsEcoule / 60f);
            int secondes = Mathf.FloorToInt(tempsEcoule % 60f);
            texteChrono.text = string.Format("{0}:{1:00}", minutes, secondes);
        }
    }

    private string FormaterChrono(float temps)
    {
        if (temps <= 0f) return "--:--.--";
        int minutes = Mathf.FloorToInt(temps / 60f);
        int secondes = Mathf.FloorToInt(temps % 60f);
        int centiemes = Mathf.FloorToInt((temps * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, secondes, centiemes);
    }

    // --- NOUVEAU : Fonction demandée par le GameManager pour forcer la remise à zéro ---
    public void ReinitialiserChrono()
    {
        tempsEcoule = 0f;
        chronoActif = false;
        aDemarre = false;
        dernierTempsAffiche = -1f;
        MettreAJourAffichage();
        TrouverJoueur();
    }

    // --- NOUVEAU : Fonction demandée par le GameManager pour forcer le départ (ex: après le 3,2,1 GO) ---
    public void DemarrerChrono()
    {
        aDemarre = true;
        chronoActif = true;
        Debug.Log("⏱️ Le chronomètre est lancé !");
    }

    public void ArreterChrono()
    {
        if (!chronoActif) return; 
        
        chronoActif = false;
        MettreAJourAffichage(); 
        
        Debug.Log("Ligne d'arrivée franchie ! Temps final : " + tempsEcoule);
        Time.timeScale = 0f;

        // 🚀 NOUVEAU : On récupère l'index du niveau actuellement joué (0, 1, 2 ou 3)
        int indexNiveau = PlayerPrefs.GetInt("NiveauSpeedrunActuel", 0);

        if (SaveManager.instance != null)
        {
            float record = SaveManager.instance.data.meilleursTempsSpeedrun[indexNiveau];
            
            if (record == 0f || tempsEcoule < record)
            {
                SaveManager.instance.data.meilleursTempsSpeedrun[indexNiveau] = tempsEcoule;
                SaveManager.instance.SauvegarderPartie();

                Debug.Log("🏆 Nouveau Record Speedrun validé pour le niveau " + (indexNiveau + 1) + " !");

                if (FirebaseManager.instance != null) 
                {
                    // 🚀 NOUVEAU : On envoie l'index avec le temps
                    FirebaseManager.instance.EnvoyerTempsSpeedrun(tempsEcoule, indexNiveau); 
                }
            }
        }

        // Mise à jour de l'UI
        if (texteTempsFinal != null) texteTempsFinal.SetText("Temps : " + FormaterChrono(tempsEcoule));
        
        if (texteMeilleurTemps != null)
        {
            float meilleurTempsLocal = SaveManager.instance != null ? SaveManager.instance.data.meilleursTempsSpeedrun[indexNiveau] : 0f;
            texteMeilleurTemps.SetText("Meilleur : " + FormaterChrono(meilleurTempsLocal));
        }

        if (panelVictoire != null) panelVictoire.SetActive(true);
    }

    public float ObtenirTemps()
    {
        return tempsEcoule;
    }
    // --- NOUVEAU : Fonction pour cacher les éléments d'UI à la volée sans recharger la scène ---
    public void AppliquerAffichageHUD(string modeDeJeu)
    {
        // Si on est en Speedrun OU en 1v1, on cache les pièces, scores, etc.
        if (modeDeJeu == "Speedrun" || modeDeJeu == "1v1")
        {
            foreach (GameObject element in elementsAMasquer)
            {
                if (element != null) element.SetActive(false);
            }
        }
        else // Sinon (Mode Normal), on les affiche
        {
            foreach (GameObject element in elementsAMasquer)
            {
                if (element != null) element.SetActive(true);
            }
        }
    }
}