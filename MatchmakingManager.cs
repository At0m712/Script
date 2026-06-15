using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using System.Collections;
using UnityEngine.Localization.Settings; 

public class MatchmakingManager : MonoBehaviour
{
    public static MatchmakingManager instance;

    private DatabaseReference dbReference;
    private bool firebaseEstPret = false;
    private bool rechercheEnCours = false;
    private bool matchLance = false; 

    private bool declencherCompteARebours = false;
    private string pseudoAdversaireTrouve = "";

    [Header("Paramètres 1v1")]
    public GameObject prefabZone1v1; 
    public GameObject menuPrincipalUI; 
    
    [Header("Position de départ 1v1")]
    public Vector3 pointDepartCourse = new Vector3(0f, 2f, 0f);

    [Header("Interface de Recherche")]
    public GameObject panelMatchmaking; 
    public TMP_Text texteJoueur1; 
    public TMP_Text texteJoueur2; 
    public TMP_Text texteStatut; 

    [Header("Interface de Fin 1v1")]
    public GameObject texteAttenteAdversaire; 
    public GameObject panelVictoire1v1; 
    public GameObject panelDefaite1v1;  
    public TMP_Text texteResultatDetaille;

    public static string monRoleActuel; 
    public static string idDeMonSalon;
    public static int seedDuNiveau; 
    // Compteur pour déclencher la publicité interstitielle
    private static int compteurPubRetour = 0;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available) {
                dbReference = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                firebaseEstPret = true;
                
                if (PlayerPrefs.GetInt("AutoStartMatchmaking", 0) == 1)
                {
                    PlayerPrefs.SetInt("AutoStartMatchmaking", 0);
                    PlayerPrefs.Save();
                    ChercherUnePartie1v1();
                }
            } else {
                Debug.LogError("Impossible d'initialiser Firebase : " + task.Result);
            }
        });
    }

    private string T(string cle)
    {
        return LocalizationSettings.StringDatabase.GetLocalizedString("TexteUI", cle);
    }

    public void ChercherUnePartie1v1()
    {
        if (rechercheEnCours) return;

        // 🛡️ SÉCURITÉ 1 : Vérification de la connexion Internet (Syndrome du métro)
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("Aucune connexion Internet détectée !");
            
            if (panelMatchmaking != null) panelMatchmaking.SetActive(true);
            if (texteStatut != null) texteStatut.text = "Pas de connexion Internet...";
            
            Invoke("AnnulerRecherche", 2f); 
            return;
        }

        if (!firebaseEstPret || dbReference == null)
        {
            Debug.LogWarning("⏳ Firebase se connecte, réessaie dans 1 seconde !");
            return;
        }

        rechercheEnCours = true; 
        matchLance = false;
        declencherCompteARebours = false;

        // 🛡️ SÉCURITÉ 2 : Nettoyage de l'ID Android
        string idAppareilSecurise = SystemInfo.deviceUniqueIdentifier.Replace(".", "").Replace("#", "").Replace("$", "").Replace("[", "").Replace("]", "");
        
        // 🛡️ CORRECTION : Fallback sur le pseudo local en cas de problème de récupération serveur
        dbReference.Child("Joueurs").Child(idAppareilSecurise).Child("pseudo").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            // On charge le pseudo local par défaut en sécurité
            string monVraiPseudo = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur"); 

            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled && task.Result.Exists)
            {
                monVraiPseudo = task.Result.Value.ToString();
            }
            else if (task.IsFaulted)
            {
                Debug.LogWarning("Impossible de récupérer le pseudo sur Firebase (Règles ou réseau). Utilisation du pseudo local.");
            }

            ContinuerRechercheAvecVraiPseudo(monVraiPseudo);
        });
    }
    private void ContinuerRechercheAvecVraiPseudo(string monPseudo)
    {
        if (panelMatchmaking != null) panelMatchmaking.SetActive(true);
        if (texteStatut != null) texteStatut.text = T("MM_RECHERCHE");
        if (texteJoueur1 != null) texteJoueur1.text = $"{monPseudo}\n({T("MM_MOI")})";
        if (texteJoueur2 != null) texteJoueur2.text = T("MM_ATTENTE");

        PlayerPrefs.SetString("ModeChoisi", "1v1");
        PlayerPrefs.Save();

        dbReference.Child("Salons_1v1").OrderByChild("etat").EqualTo("EnAttente").LimitToFirst(1)
            .GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) {
                Debug.LogError("Erreur Firebase : " + task.Exception.ToString());
                rechercheEnCours = false;
                if (panelMatchmaking != null) panelMatchmaking.SetActive(false);
                return;
            }

            DataSnapshot snapshot = task.Result;

            if (snapshot.Exists && snapshot.ChildrenCount > 0)
            {
                foreach (DataSnapshot salon in snapshot.Children)
                {
                    string pseudoJ1 = salon.Child("Joueur1").Child("pseudo").Value?.ToString() ?? "Joueur 1";
                    RejoindreSalonExistant(salon.Key, salon.Child("seedNiveau").Value.ToString(), pseudoJ1, monPseudo);
                    break; 
                }
            }
            else
            {
                CreerNouveauSalon(monPseudo);
            }
        });
    }

    private void CreerNouveauSalon(string monPseudo)
    {
        monRoleActuel = "Joueur1";
        idDeMonSalon = dbReference.Child("Salons_1v1").Push().Key; 
        seedDuNiveau = Random.Range(10000, 99999); 

        dbReference.Child("Salons_1v1").Child(idDeMonSalon).Child("etat").SetValueAsync("EnAttente");
        dbReference.Child("Salons_1v1").Child(idDeMonSalon).Child("seedNiveau").SetValueAsync(seedDuNiveau);
        
        dbReference.Child("Salons_1v1").Child(idDeMonSalon).Child("Joueur1").Child("pseudo").SetValueAsync(monPseudo); 
        
        SurveillerArriveeJoueur2();
    }

    private void RejoindreSalonExistant(string idSalonTrouve, string seedTrouvee, string pseudoJoueur1, string monPseudo)
    {
        monRoleActuel = "Joueur2";
        idDeMonSalon = idSalonTrouve;
        seedDuNiveau = int.Parse(seedTrouvee); 

        dbReference.Child("Salons_1v1").Child(idDeMonSalon).Child("Joueur2").Child("pseudo").SetValueAsync(monPseudo); 
        dbReference.Child("Salons_1v1").Child(idDeMonSalon).Child("etat").SetValueAsync("EnCours");

        if (texteJoueur1 != null) texteJoueur1.text = pseudoJoueur1;
        if (texteJoueur2 != null) texteJoueur2.text = $"{monPseudo}\n({T("MM_MOI")})";
        
        StartCoroutine(CompteAReboursAvantLancement());
    }

    private void SurveillerArriveeJoueur2()
    {
        dbReference.Child("Salons_1v1").Child(idDeMonSalon).ValueChanged += (sender, args) =>
        {
            if (!args.Snapshot.Exists) return;

            string etat = args.Snapshot.Child("etat").Value?.ToString();
            
            if (etat == "EnCours" && !matchLance)
            {
                matchLance = true; 
                pseudoAdversaireTrouve = args.Snapshot.Child("Joueur2").Child("pseudo").Value?.ToString() ?? "JoueurB";
                declencherCompteARebours = true;
            }
        };
    }

    void Update()
    {
        if (declencherCompteARebours)
        {
            declencherCompteARebours = false;
            if (texteJoueur2 != null) texteJoueur2.text = pseudoAdversaireTrouve;
            StartCoroutine(CompteAReboursAvantLancement());
        }
    }

    private IEnumerator CompteAReboursAvantLancement()
    {
        rechercheEnCours = false; 

        if (texteStatut != null) texteStatut.text = T("MM_TROUVE");
        yield return new WaitForSecondsRealtime(1.5f);

        string formatLancement = T("MM_LANCEMENT"); 

        if (texteStatut != null) texteStatut.text = string.Format(formatLancement, 3);
        yield return new WaitForSecondsRealtime(1f);

        if (texteStatut != null) texteStatut.text = string.Format(formatLancement, 2);
        yield return new WaitForSecondsRealtime(1f);

        if (texteStatut != null) texteStatut.text = string.Format(formatLancement, 1);
        yield return new WaitForSecondsRealtime(1f);

        if (panelMatchmaking != null) panelMatchmaking.SetActive(false);
        
        LancerLaPartieDansLaMemeScene();
    }

    private void LancerLaPartieDansLaMemeScene()
    {
        if (LevelGenerator.instance != null)
        {
            LevelGenerator.instance.StopperEtNettoyerClassique();
        }

        Instantiate(prefabZone1v1, Vector3.zero, Quaternion.identity);

        if (GenerateurNiveau.instance != null)
        {
            GenerateurNiveau.instance.GenererLeNiveau1v1();
        }

        if (ThemeManager.instance != null)
        {
            ThemeManager.instance.LancerTransitionVersJeu();
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.LancerJoueurEn1v1(pointDepartCourse);
        }
    }

    public void QuitterEtNettoyerSalon()
    {
        // 1. Nettoyage du salon sur Firebase
        if (!string.IsNullOrEmpty(idDeMonSalon) && dbReference != null)
        {
            dbReference.Child("Salons_1v1").Child(idDeMonSalon).RemoveValueAsync();
            Debug.Log("🧹 Salon supprimé de Firebase.");
        }
        
        idDeMonSalon = "";
        monRoleActuel = "";
        if (GameManager.instance != null && GameManager.instance.joueurRb != null) GameManager.instance.joueurRb.isKinematic = false;

        // ==========================================
        // 2. GESTION DE LA PUBLICITÉ INTERSTITIELLE
        // ==========================================
        compteurPubRetour++; // On incrémente le compteur

        if (compteurPubRetour >= 3)
        {
            compteurPubRetour = 0; // Remise à zéro
            if (AdMobManager.instance != null && AdMobManager.instance.IsInterstitialReady())
            {
                Debug.Log("📺 Affichage de la pub interstitielle (3ème clic atteint) !");
                AdMobManager.instance.ShowInterstitialAd();
            }
        }

        // 3. Retour au menu (S'exécute après ou pendant la pub selon ton GameManager)
        if (GameManager.instance != null) GameManager.instance.BoutonRetourMenu();
    }

    public void AnnulerRecherche()
    {
        StopAllCoroutines();
        rechercheEnCours = false;
        matchLance = false;

        if (panelMatchmaking != null) panelMatchmaking.SetActive(false);

        // 1. Nettoyage du salon sur Firebase
        if (!string.IsNullOrEmpty(idDeMonSalon) && dbReference != null)
        {
            dbReference.Child("Salons_1v1").Child(idDeMonSalon).RemoveValueAsync();
            Debug.Log("🧹 Recherche annulée : Salon supprimé du serveur.");
        }

        idDeMonSalon = "";
        monRoleActuel = "";

        // ==========================================
        // 2. GESTION DE LA PUBLICITÉ INTERSTITIELLE
        // ==========================================
        compteurPubRetour++; // On incrémente le compteur

        if (compteurPubRetour >= 3)
        {
            compteurPubRetour = 0; // Remise à zéro
            if (AdMobManager.instance != null && AdMobManager.instance.IsInterstitialReady())
            {
                Debug.Log("📺 Affichage de la pub interstitielle (3ème clic atteint) !");
                AdMobManager.instance.ShowInterstitialAd();
            }
        }
    }

    // =================================================================
    // 🛡️ SÉCURITÉ ANDROID : GESTION DE FERMETURE SAUVAGE (Glissement)
    // =================================================================
    
    // Appelée quand le joueur met le jeu en arrière-plan (Home, Menu des apps...)
    // =================================================================
    // 🛡️ SÉCURITÉ MOBILE : GESTION DE LA MISE EN ARRIÈRE-PLAN
    // =================================================================
    
    // Appelée quand le joueur met le jeu en arrière-plan (Home, Menu des apps...)
    void OnApplicationPause(bool isPaused)
    {
        // Dès que l'application passe en arrière-plan (même 1 seconde)
        if (isPaused)
        {
            // Si le joueur était en pleine recherche de partie
            if (rechercheEnCours)
            {
                Debug.Log("📱 Mise en pause détectée : Annulation de la recherche 1v1 et fermeture de l'UI.");
                
                // 1. On appelle ta fonction existante pour être sûr que le salon Firebase est bien supprimé 
                // et que le compteur de pub est mis à jour
                AnnulerRecherche();

                // 2. Sécurité visuelle : on force la désactivation immédiate du panel de matchmaking
                if (panelMatchmaking != null)
                {
                    panelMatchmaking.SetActive(false);
                }

                // 3. On réinitialise les sécurités locales pour éviter les bugs au retour sur l'app
                rechercheEnCours = false;
                matchLance = false;
                declencherCompteARebours = false;
                
                if (texteStatut != null) 
                {
                    texteStatut.text = "";
                }
            }
            else
            {
                // Si le joueur n'était pas en recherche, on utilise quand même le nettoyage de secours 
                // au cas où un salon fantôme traînerait.
                NettoyerSalonEnDernierRecours();
            }
        }
    }

    // Appelée quand on quitte le jeu proprement (plus rare sur mobile)
    void OnApplicationQuit()
    {
        NettoyerSalonEnDernierRecours();
    }

    private void NettoyerSalonEnDernierRecours()
    {
        // On ne détruit le salon QUE si on était en pleine recherche (pas si on est déjà en train de jouer)
        if (!string.IsNullOrEmpty(idDeMonSalon) && dbReference != null && !matchLance)
        {
            dbReference.Child("Salons_1v1").Child(idDeMonSalon).RemoveValueAsync();
            Debug.Log("🧹 Android : Application mise en arrière-plan, Salon annulé et supprimé.");
            
            // On réinitialise pour pouvoir chercher à nouveau si le joueur revient sur l'app
            rechercheEnCours = false;
            idDeMonSalon = "";
            monRoleActuel = "";
        }
    }
}