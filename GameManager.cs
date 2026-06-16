using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem; 
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static int currentLevel;

    public static SafeInt vies = 3; 
    public static SafeInt argentTotal = 0; 
    private SafeInt scoreActuel = 0;       
    private SafeInt meilleurScore = 0;  
    // Compteur pour la pub interstitielle du menu
    private static int compteurPubRetourMenu = 0;

    [Header("Interface Globale")]
    public GameObject panelDefaite; 

    [Header("Interface Pause")]
    public GameObject panelPause;      
    public GameObject panelParametres; 
    private bool estEnPause = false;

    [Header("UI Textes")]
    public TMP_Text texteArgent; 
    public TMP_Text texteScore;  
    public TMP_Text texteMeilleurScore; 
    public TMP_Text texteNiveauHUD; 
    
    [Header("Textes du Panneau Defaite")]
    public TMP_Text texteScoreDefaite; 
    public TMP_Text texteMeilleurScoreDefaite; 

    [Header("Joueur et Respawn")]
    public Vector3 pointDeRespawn = new Vector3(0f, 2f, 0f);
    [Header("Interface des Vies")]
    public GameObject[] coeursUI; // Tableau qui contiendra nos 5 images de cœurs

    // --- CORRECTION ARCHITECTURE : Le joueur est en accès public pour les autres scripts ! ---
    public GameObject joueurActuel { get; private set; }
    public Rigidbody joueurRb { get; private set; }

    private InputAction pauseAction;
    private bool doitRevivre = false;

    void Awake()
    {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }

        pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.performed += ctx => BasculerPause();
    }

    void OnEnable() { pauseAction.Enable(); }
    void OnDisable() { pauseAction.Disable(); }

    void Start()
    {
        ChercherLeJoueur();

        currentLevel = SaveManager.instance.data.niveau;
        meilleurScore = SaveManager.instance.data.meilleurScore;
        argentTotal = SaveManager.instance.data.argentTotal;

        if(panelDefaite != null) panelDefaite.SetActive(false);

        if (currentLevel == 1)
        {
            scoreActuel = 0;
            SaveManager.instance.data.scoreSession = 0; 
        }
        else
        {
            scoreActuel = SaveManager.instance.data.scoreSession;
        }
        
        ActualiserAffichageCoeurs();
        MettreAJourUI();
        MettreAJourNiveau();
        
    }

    // --- NOUVEAU : Fonction centralisée pour trouver le joueur ---
    public void ChercherLeJoueur()
    {
        joueurActuel = GameObject.FindGameObjectWithTag("Player");
        if (joueurActuel != null)
        {
            joueurRb = joueurActuel.GetComponent<Rigidbody>();
        }
    }

    private void BasculerPause()
    {
        if (!ThemeManager.jeuEstLance || (panelDefaite != null && panelDefaite.activeSelf)) return;
        if (estEnPause) ReprendreJeu();
        else MettreEnPause();
    }

    public void MettreEnPause()
    {
        estEnPause = true;
        if (panelPause != null) panelPause.SetActive(true);
        if (panelParametres != null) panelParametres.SetActive(false);
        Time.timeScale = 0f; 
    }

    public void ReprendreJeu()
    {
        estEnPause = false;
        if (panelPause != null) panelPause.SetActive(false);
        if (panelParametres != null) panelParametres.SetActive(false);
        Time.timeScale = 1f; 
    }

    public void BoutonOuvrirParametres()
    {
        // On cache la pause SI elle était ouverte, puis on affiche les paramètres
        if (panelPause != null) panelPause.SetActive(false);
        if (panelParametres != null) panelParametres.SetActive(true);
    }

    public void BoutonFermerParametres()
    {
        // On ferme les paramètres
        if (panelParametres != null) panelParametres.SetActive(false);
        
        // LA MAGIE ICI : On ne rouvre le panneau Pause QUE si le joueur était en pleine partie
        if (ThemeManager.jeuEstLance && estEnPause)
        {
            if (panelPause != null) panelPause.SetActive(true);
        }
    }

    // --- CORRECTION ARCHITECTURE : Seul le GameManager gère l'argent ---
    public void AjouterArgent(int montant)
    {
        // NOUVEAU : Si le X2 est actif, on double l'argent !
        if (PowerUpManager.instance != null && PowerUpManager.instance.x2Actif) montant *= 2;

        argentTotal += montant;
        SaveManager.instance.data.argentTotal = argentTotal;
        MettreAJourUI(); 
    }

    // NOUVEAU : Pour les achats dans la boutique
    public bool DepenserArgent(int montant)
    {
        if (argentTotal >= montant)
        {
            argentTotal -= montant;
            SaveManager.instance.data.argentTotal = argentTotal;
            SaveManager.instance.SauvegarderPartie(); // Achat = sauvegarde obligatoire
            MettreAJourUI();
            return true; // Transaction validée
        }
        return false; // Pas assez d'argent
    }

    // (L'ancienne fonction MettreAJourArgentDirectement a été supprimée, on utilise DepenserArgent maintenant)

public void MettreAJourNiveau()
    {
        SaveManager.instance.data.niveau = currentLevel;
        SaveManager.instance.SauvegarderPartie();  

        if (texteNiveauHUD != null) 
        {
            // 1. On récupère la traduction brute : "Niveau {0}"
            string texteTraduit = LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", "JEU_NIVEAU");
            
            // 2. On assemble le texte et le chiffre de façon 100% sécurisée
            texteNiveauHUD.text = string.Format(texteTraduit, currentLevel);
        }
    }

    public void AjouterScore(int points)
    {
        // NOUVEAU : Si le X2 est actif, on double les points !
        if (PowerUpManager.instance != null && PowerUpManager.instance.x2Actif) points *= 2;

        scoreActuel += points;
        SaveManager.instance.data.scoreSession = scoreActuel;

        if (scoreActuel > meilleurScore)
        {
            meilleurScore = scoreActuel;
            SaveManager.instance.data.meilleurScore = meilleurScore;
        }
        
        MettreAJourUI(); 
    }
    // Fonction pour le bonus de Vie Supplémentaire (Max 5)
    public void AjouterVie()
    {
        // Si le joueur a déjà 5 vies ou plus, on ignore le bonus
        if (vies >= 5) 
        {
            Debug.Log("Déjà au max de vies (5) !");
            return; 
        }

        // Sinon, on ajoute la vie normalement
        vies += 1;
        ActualiserAffichageCoeurs();
        
        // On sauvegarde le nouvel état de la partie
        SaveManager.instance.SauvegarderPartie();
        
        // C'est ici que tu mettras à jour l'affichage de ton UI (ex: cœurs) si tu en as une !
        MettreAJourUI(); 
    }
    public void MettreAJourUI()
    {
        if (texteArgent != null) texteArgent.SetText("{0}", argentTotal);
        if (texteScore != null)  texteScore.SetText("Score : {0}", scoreActuel);
        if (texteMeilleurScore != null) texteMeilleurScore.SetText("Record : {0}", meilleurScore);
    }

    public void WinLevel()
    {
        currentLevel++;
        SaveManager.instance.data.niveau = currentLevel;
        SaveManager.instance.SauvegarderPartie(); 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool PerdreVie()
    {
        vies--; 
        ActualiserAffichageCoeurs();
        SaveManager.instance.SauvegarderPartie(); 

        if (vies > 0) return true; 
        else
        {
            AfficherDefaite();
            return false; 
        }
    }
    private string FormaterChrono(float temps)
    {
        if (temps <= 0f) return "--:--.--"; // S'il n'y a pas encore de record
        int minutes = Mathf.FloorToInt(temps / 60f);
        int secondes = Mathf.FloorToInt(temps % 60f);
        int centiemes = Mathf.FloorToInt((temps * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, secondes, centiemes);
    }
    void AfficherDefaite()
    {
        string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (modeChoisi == "Speedrun")
        {
            float tempsActuel = 0f;
            if (ChronoManager.instance != null) tempsActuel = ChronoManager.instance.ObtenirTemps();

            int record = 0; // 👉 C'est un int maintenant
            int indexNiveau = PlayerPrefs.GetInt("NiveauSpeedrunActuel", 0);
            if (SaveManager.instance != null) record = SaveManager.instance.data.meilleursTempsSpeedrun[indexNiveau];

            if (texteScoreDefaite != null) texteScoreDefaite.SetText("Temps : " + FormaterChrono(tempsActuel));
            if (texteMeilleurScoreDefaite != null) texteMeilleurScoreDefaite.SetText("Record : " + FormaterChronoEntier(record));
        }
        else
        {
            // --- AFFICHAGE MODE NORMAL ---
            if (texteScoreDefaite != null) texteScoreDefaite.SetText("Score : {0}", scoreActuel);
            if (texteMeilleurScoreDefaite != null) texteMeilleurScoreDefaite.SetText("Meilleur Score : {0}", meilleurScore);
            
            // 🛡️ SÉCURITÉ ET ENVOI FIREBASE
            if (FirebaseManager.instance != null) 
            {
                // On n'envoie au Leaderboard public QUE si c'est notre meilleur score
                if (scoreActuel >= meilleurScore && scoreActuel > 0)
                {
                    FirebaseManager.instance.EnvoyerScore(scoreActuel);
                }
                
                // Les Analytics peuvent être envoyées à chaque mort, c'est normal
                FirebaseManager.instance.AnalyserMortJoueur(currentLevel, scoreActuel); 
            }

            // On force la sauvegarde locale pour que l'espion ProfileManager valide le nouveau profil
            if (SaveManager.instance != null)
            {
                SaveManager.instance.SauvegarderPartie();
            }
        }

        // On affiche le panel et on met le jeu en pause
        if(panelDefaite != null) panelDefaite.SetActive(true);
        Time.timeScale = 0f; 
    }

    public void BoutonRecommencerNiveau()
    {
        vies = 3; 
        SaveManager.instance.data.niveau = 1;       
        SaveManager.instance.data.scoreSession = 0; 
        SaveManager.instance.SauvegarderPartie();                    

        ThemeManager.jeuEstLance = true;      
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BoutonRetourMenu()
    {
        vies = 3;
        SaveManager.instance.data.niveau = 1; 
        SaveManager.instance.data.scoreSession = 0; 
        SaveManager.instance.SauvegarderPartie();   

        ThemeManager.jeuEstLance = false; 
        Time.timeScale = 1f; 

        // ==========================================
        // 📺 GESTION DE LA PUBLICITÉ INTERSTITIELLE
        // ==========================================
        compteurPubRetourMenu++; // On incrémente le compteur

        if (compteurPubRetourMenu >= 3)
        {
            compteurPubRetourMenu = 0; // Remise à zéro
            
            // On vérifie que le script AdMob est là et que la pub a fini de charger
            if (AdMobManager.instance != null && AdMobManager.instance.IsInterstitialReady())
            {
                Debug.Log("📺 Affichage de la pub interstitielle (3ème retour au menu) !");
                AdMobManager.instance.ShowInterstitialAd();
            }
        }

        // On recharge la scène à la toute fin (la pub s'affichera en superposition sans problème)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Revivre()
    {
        doitRevivre = true; 
    }

    void Update()
    {
        if (doitRevivre)
        {
            doitRevivre = false; 
            ExecuterResurrection();
        }
    }

    public void ExecuterResurrection()
    {
        vies = 1;
        ActualiserAffichageCoeurs();

        if (panelDefaite != null) panelDefaite.SetActive(false);
        Time.timeScale = 1f;

        // On utilise la référence sécurisée
        if (joueurActuel == null) ChercherLeJoueur();

        if (joueurActuel != null)
        {
            // --- CORRECTION : On téléporte le RIGIDBODY en priorité pour forcer la physique ---
            if (joueurRb != null)
            {
                joueurRb.position = pointDeRespawn; // Téléportation physique immédiate
                joueurRb.linearVelocity = Vector3.zero;  // On stoppe net sa vitesse de chute
                joueurRb.angularVelocity = Vector3.zero; // On stoppe sa rotation
            }

            // Par sécurité, on aligne aussi le transform classique
            joueurActuel.transform.position = pointDeRespawn;
            joueurActuel.transform.rotation = Quaternion.identity;
        }
        
        SaveManager.instance.SauvegarderPartie();
    }
    public void ActualiserAffichageCoeurs()
    {
        // On parcourt la liste de nos 5 cœurs
        for (int i = 0; i < coeursUI.Length; i++)
        {
            // On force le cœur à rester allumé dans tous les cas
            coeursUI[i].SetActive(true);

            // On récupère le composant "Image" de ce cœur précis
            Image imageCoeur = coeursUI[i].GetComponent<Image>();

            if (imageCoeur != null)
            {
                if (i < vies)
                {
                    // CŒUR PLEIN : On lui met sa couleur d'origine (100% visible)
                    imageCoeur.color = Color.white; 
                }
                else
                {
                    // CŒUR VIDE : On le met en Noir (R:0, G:0, B:0) à 50% de transparence (A:0.5f)
                    imageCoeur.color = new Color(0f, 0f, 0f, 0.5f); 
                }
            }
        }
    }
    // --- NOUVEAU : Fonction spéciale pour lancer le joueur en 1v1 ---
    public void LancerJoueurEn1v1(Vector3 pointDeDepart1v1)
    {
        // 1. On s'assure d'avoir le joueur
        if (joueurActuel == null) ChercherLeJoueur();

        if (joueurActuel != null)
        {
            // 2. On le téléporte au début du niveau 1v1 et on fige sa physique
            if (joueurRb != null)
            {
                joueurRb.position = pointDeDepart1v1;
                joueurRb.linearVelocity = Vector3.zero;  
                joueurRb.angularVelocity = Vector3.zero; 
            }
            joueurActuel.transform.position = pointDeDepart1v1;
            joueurActuel.transform.rotation = Quaternion.identity;
        }

        // 3. On signale que le jeu commence officiellement !
        ThemeManager.jeuEstLance = true;
        Time.timeScale = 1f;

        // 4. Si tu as ton script ChronoManager, c'est ici qu'on le remet à zéro !
        if (ChronoManager.instance != null)
        {
            // 👉 LA NOUVELLE LIGNE EST ICI : On nettoie l'écran !
            ChronoManager.instance.AppliquerAffichageHUD("1v1");
            
            ChronoManager.instance.ReinitialiserChrono();
            ChronoManager.instance.DemarrerChrono();
        }
    }
    private string FormaterChronoEntier(int centiemesTotaux)
    {
        if (centiemesTotaux <= 0) return "--:--.--";
        int minutes = (centiemesTotaux / 100) / 60;
        int secondes = (centiemesTotaux / 100) % 60;
        int centiemes = centiemesTotaux % 100;
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, secondes, centiemes);
    }
}