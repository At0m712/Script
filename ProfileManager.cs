using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using Firebase.Auth; 
using System; 

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
    private string idUniqueAppareil = ""; 
    
    // 🛡️ NOUVEAU : Ce booléen bloque toute sauvegarde tant que Firebase n'est pas 100% prêt
    private bool profilEstSynchronise = false; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
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

        if (FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            idUniqueAppareil = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            PlayerPrefs.SetString("MonIDFirebase", idUniqueAppareil);
            PlayerPrefs.Save();
            
            // 🛡️ CORRECTION : On laisse 1 seconde à la base de données pour se synchroniser avec l'Auth
            Invoke("InitialiserEtSynchroniser", 1f);
            return;
        }

        FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 Erreur d'authentification serveur : " + task.Exception);
                return;
            }

            idUniqueAppareil = task.Result.User.UserId;
            PlayerPrefs.SetString("MonIDFirebase", idUniqueAppareil);
            PlayerPrefs.Save();
            
            Debug.Log("📂 [ProfileManager] Authentification 100% validée avec ID : " + idUniqueAppareil);
            
            // 🛡️ CORRECTION : Même chose ici, on patiente avant de requêter
            Invoke("InitialiserEtSynchroniser", 1f);
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

        if (string.IsNullOrEmpty(idUniqueAppareil)) return; 

        Debug.Log("📍 [ProfileManager] 4. Envoi de la requête de lecture au serveur...");

        dbReference.Child("Joueurs").Child(idUniqueAppareil).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            // 🛡️ CORRECTION MAJEURE : On gère le refus de permission (Internal task faulted).
            // Au lieu d'écraser la sauvegarde en pensant qu'elle n'existe pas, on patiente et on retente !
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("⚠️ Lecture bloquée (Permissions ou Jeton en cours de validation). Réessai dans 1 seconde...");
                Invoke("InitialiserEtSynchroniser", 1f);
                return;
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("📍 [ProfileManager] 5. Profil cloud trouvé. Déchiffrage des données...");
                ProfilJoueur profilCloud = new ProfilJoueur();
                
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

            // 🟢 FEU VERT : La base de données a répondu favorablement, on autorise les écritures !
            profilEstSynchronise = true;

            StopAllCoroutines();
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

            // 🛡️ SÉCURITÉ : On n'écrit jamais sur le Cloud si la synchro initiale n'est pas passée
            if (ilYaEuUnChangement && profilEstSynchronise)
            {
                EnvoyerVersFirebase();
            }
        }
    }

    private void EnvoyerVersFirebase()
    {
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
        SaveManager.instance.data.argentTotal = profilCloud.nbPieces;
        SaveManager.instance.data.meilleurScore = profilCloud.meilleurScoreClassique;
        SaveManager.instance.data.meilleurTempsSpeedrun = profilCloud.meilleurChronoSpeedrun / 100f;

        monProfil.nbPieces = profilCloud.nbPieces;
        monProfil.meilleurScoreClassique = profilCloud.meilleurScoreClassique;
        monProfil.meilleurChronoSpeedrun = profilCloud.meilleurChronoSpeedrun;

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

    void OnApplicationPause(bool isPaused)
    {
        // 🛡️ SÉCURITÉ : La mise en pause au démarrage du jeu ne forcera plus un faux envoi
        if (isPaused && profilEstSynchronise)
        {
            Debug.Log("📱 Android : Mise en pause détectée, sauvegarde d'urgence autorisée !");
            
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