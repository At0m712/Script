using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using Firebase.Auth; 
using System; // 🔴 INDISPENSABLE POUR LA LECTURE SÉCURISÉE DES DONNÉES

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance;

    [System.Serializable]
    public class ProfilJoueur
    {
        public string pseudo = "joueur";
        public int nbPieces = 0;
        public int meilleurScoreClassique = 0;
        public int meilleurChronoSpeedrun = 0; 
    }

    public ProfilJoueur monProfil = new ProfilJoueur();

    private DatabaseReference dbReference;
    
    // 🛡️ CORRECTION : L'ID est vide par défaut, on attend impérativement l'autorisation Firebase !
    private string idUniqueAppareil = ""; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
            
            // 🧹 NETTOYAGE : J'ai complètement supprimé l'ancien code SystemInfo.deviceUniqueIdentifier
            // C'est lui qui s'emmêlait les pinceaux avec le vrai compte et créait des dossiers en double !
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("📍 [ProfileManager] 1. Lancement et vérification Firebase...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available) 
            {
                Debug.Log("📍 [ProfileManager] 2. Dépendances Firebase OK !");
                dbReference = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                
                Debug.Log("📍 [ProfileManager] 3. dbReference créée. Lancement de l'authentification...");
                AttendreAuthentification();
            } 
            else 
            {
                Debug.LogError("🚨 [ProfileManager] Erreur Firebase : " + task.Result);
            }
        });
    }

    private void AttendreAuthentification()
    {
        Debug.Log("⏳ [ProfileManager] Récupération du jeton d'authentification...");

        // 🛡️ OPTIMISATION : Si Firebase a déjà authentifié le joueur en arrière-plan, on récupère direct son ID !
        if (FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            idUniqueAppareil = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            PlayerPrefs.SetString("MonIDFirebase", idUniqueAppareil);
            PlayerPrefs.Save();
            InitialiserEtSynchroniser();
            return;
        }

        // Sinon, on sécurise une connexion 
        FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 Erreur d'authentification serveur : " + task.Exception);
                return;
            }

            // On récupère le VRAI jeton validé par le serveur
            idUniqueAppareil = task.Result.User.UserId;
            PlayerPrefs.SetString("MonIDFirebase", idUniqueAppareil);
            PlayerPrefs.Save();
            
            Debug.Log("📂 [ProfileManager] Authentification 100% validée avec ID : " + idUniqueAppareil);
            InitialiserEtSynchroniser();
        });
    }

    private void InitialiserEtSynchroniser()
    {
        if (SaveManager.instance != null)
        {
            monProfil.nbPieces = SaveManager.instance.data.argentTotal;
            monProfil.meilleurScoreClassique = SaveManager.instance.data.meilleurScore;
            monProfil.meilleurChronoSpeedrun = Mathf.FloorToInt(SaveManager.instance.data.meilleurTempsSpeedrun * 100f);
        }
        monProfil.pseudo = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");

        if (string.IsNullOrEmpty(idUniqueAppareil)) return; // Sécurité anti-dossier fantôme

        Debug.Log("📍 [ProfileManager] 4. Envoi de la requête de lecture au serveur...");

        dbReference.Child("Joueurs").Child(idUniqueAppareil).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("⚠️ Lecture Firebase bloquée (IsFaulted). Création forcée ! Détail : " + task.Exception);
                EnvoyerVersFirebase();
                StartCoroutine(TraqueurAutomatiqueDeChangements());
                return;
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("📍 [ProfileManager] 5. Profil cloud trouvé. Déchiffrage des données...");
                ProfilJoueur profilCloud = new ProfilJoueur();
                
                // 🛡️ CORRECTION MAGIQUE : On force la lecture manuelle nœud par nœud.
                // Cela empêche JsonUtility de planter le jeu quand tu modifies le nombre de pièces à la main !
                try
                {
                    if (task.Result.HasChild("nbPieces")) 
                        profilCloud.nbPieces = Convert.ToInt32(task.Result.Child("nbPieces").Value);
                        
                    if (task.Result.HasChild("meilleurScoreClassique")) 
                        profilCloud.meilleurScoreClassique = Convert.ToInt32(task.Result.Child("meilleurScoreClassique").Value);
                        
                    if (task.Result.HasChild("meilleurChronoSpeedrun")) 
                        profilCloud.meilleurChronoSpeedrun = Convert.ToInt32(task.Result.Child("meilleurChronoSpeedrun").Value);
                        
                    if (task.Result.HasChild("pseudo")) 
                        profilCloud.pseudo = task.Result.Child("pseudo").Value.ToString();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("⚠️ Erreur mineure lors de la lecture des données : " + e.Message);
                }

                RestaurerSauvegardeSiBesoin(profilCloud);
            }
            else 
            {
                Debug.Log("📍 [ProfileManager] Profil introuvable. Création d'un nouveau profil !");
                EnvoyerVersFirebase();
            }

            StartCoroutine(TraqueurAutomatiqueDeChangements());
        });
    }

    private IEnumerator TraqueurAutomatiqueDeChangements()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);

            if (SaveManager.instance == null) continue;

            bool ilYaEuUnChangement = false;

            if (SaveManager.instance.data.argentTotal != monProfil.nbPieces)
            {
                monProfil.nbPieces = SaveManager.instance.data.argentTotal;
                ilYaEuUnChangement = true;
            }

            if (SaveManager.instance.data.meilleurScore != monProfil.meilleurScoreClassique)
            {
                monProfil.meilleurScoreClassique = SaveManager.instance.data.meilleurScore;
                ilYaEuUnChangement = true;
            }

            int chronoSpeedrunLocalEnEntier = Mathf.FloorToInt(SaveManager.instance.data.meilleurTempsSpeedrun * 100f);
            if (chronoSpeedrunLocalEnEntier != monProfil.meilleurChronoSpeedrun)
            {
                monProfil.meilleurChronoSpeedrun = chronoSpeedrunLocalEnEntier;
                ilYaEuUnChangement = true;
            }

            string pseudoActuel = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
            if (pseudoActuel != monProfil.pseudo)
            {
                monProfil.pseudo = pseudoActuel;
                ilYaEuUnChangement = true;
            }

            if (ilYaEuUnChangement)
            {
                EnvoyerVersFirebase();
            }
        }
    }

    private void EnvoyerVersFirebase()
    {
        // 🛡️ On s'assure de ne JAMAIS envoyer de données si l'ID Firebase n'est pas rempli !
        if (dbReference != null && !string.IsNullOrEmpty(idUniqueAppareil))
        {
            string jsonProfil = JsonUtility.ToJson(monProfil);
            
            dbReference.Child("Joueurs").Child(idUniqueAppareil).SetRawJsonValueAsync(jsonProfil).ContinueWithOnMainThread(task => 
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("🚨 ERREUR FIREBASE ANDROID (Création Dossier bloquée) : " + task.Exception.ToString());
                }
                else
                {
                    Debug.Log("✅ Dossier Joueur mis à jour avec succès sur le Cloud !");
                }
            });
        }
    }

    private void RestaurerSauvegardeSiBesoin(ProfilJoueur profilCloud)
    {
        // 1. Restaurer les pièces et scores dans la mémoire locale
        SaveManager.instance.data.argentTotal = profilCloud.nbPieces;
        SaveManager.instance.data.meilleurScore = profilCloud.meilleurScoreClassique;
        SaveManager.instance.data.meilleurTempsSpeedrun = profilCloud.meilleurChronoSpeedrun / 100f;

        monProfil.nbPieces = profilCloud.nbPieces;
        monProfil.meilleurScoreClassique = profilCloud.meilleurScoreClassique;
        monProfil.meilleurChronoSpeedrun = profilCloud.meilleurChronoSpeedrun;

        // 2. Restaurer et appliquer le Pseudo
        if (!string.IsNullOrEmpty(profilCloud.pseudo) && profilCloud.pseudo != "joueur")
        {
            PlayerPrefs.SetString("MonPseudoFirebase", profilCloud.pseudo);
            PlayerPrefs.Save();
            monProfil.pseudo = profilCloud.pseudo;

            if (FirebaseManager.instance != null) 
            {
                FirebaseManager.instance.DefinirPseudo(profilCloud.pseudo);
            }
        }

        SaveManager.instance.SauvegarderPartie();

        // 3. Synchroniser le jeu en direct
        if (GameManager.instance != null)
        {
            GameManager.argentTotal = profilCloud.nbPieces; 
            GameManager.instance.MettreAJourUI();           
        }

        if (ThemeManager.instance != null)
        {
            ThemeManager.instance.RafraichirAffichageArgent(); 
        }
    }

    // =================================================================
    // 🛡️ SÉCURITÉ ANDROID : SAUVEGARDE D'URGENCE
    // =================================================================
    
    void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            Debug.Log("📱 Android : Mise en pause détectée, sauvegarde d'urgence forcée !");
            
            if (SaveManager.instance != null)
            {
                monProfil.nbPieces = SaveManager.instance.data.argentTotal;
                monProfil.meilleurScoreClassique = SaveManager.instance.data.meilleurScore;
                monProfil.meilleurChronoSpeedrun = Mathf.FloorToInt(SaveManager.instance.data.meilleurTempsSpeedrun * 100f);
                monProfil.pseudo = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
                
                SaveManager.instance.SauvegarderPartie();
            }

            EnvoyerVersFirebase();
        }
    }
}