using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance;

    private DatabaseReference dbReference;
    private FirebaseAuth auth;
    private string uidJoueur = "";
    
    private bool bloqueEcouteTemporairement = false; 

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        Debug.Log("📍 [ProfileManager] Initialisation UNIQUE de Firebase...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available) 
            {
                FirebaseDatabase instanceDB = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/");
                instanceDB.SetPersistenceEnabled(true);
                dbReference = instanceDB.RootReference;
                
                auth = FirebaseAuth.DefaultInstance;

                if (GooglePlayManager.instance != null) GooglePlayManager.instance.LancerConnexionGoogleEtFirebase(auth);
                else ConnecterAnonymement();
            } 
            else 
            {
                Debug.LogError("🚨 Erreur Dépendances Firebase : " + task.Result);
            }
        });
    }

    public void ConnecterAnonymement()
    {
        if (auth.CurrentUser != null)
        {
            DemarrerSynchronisation(auth.CurrentUser.UserId);
            return;
        }

        Debug.Log("⏳ [ProfileManager] Tentative de connexion Firebase Anonyme de secours...");
        
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 [CRITIQUE] Firebase a refusé la connexion Anonyme ! Avez-vous activé le fournisseur 'Anonyme' dans Firebase > Authentication > Sign-in method ? L'erreur exacte est : " + task.Exception);
                return;
            }
            
            Debug.Log("✅ [ProfileManager] Connecté Anonymement avec succès ! UID : " + task.Result.User.UserId);
            DemarrerSynchronisation(task.Result.User.UserId);
        });
    }

    public void DemarrerSynchronisation(string uid)
    {
        uidJoueur = uid;
        Debug.Log("📍 [ProfileManager] Lancement de l'écoute Serveur pour l'UID : " + uidJoueur);

        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.ActiverManagerApresConnexion(uidJoueur);
        }

        DatabaseReference dataRef = dbReference.Child("Joueurs").Child(uidJoueur).Child("dataComplete");
        dataRef.ValueChanged += SurChangementServeur;
    }

    private void SurChangementServeur(object sender, ValueChangedEventArgs args)
    {
        if (bloqueEcouteTemporairement) return; 

        if (args.DatabaseError != null)
        {
            Debug.LogError("🚨 [CRITIQUE DB] Firebase refuse de lire le dossier. Vos règles de base de données (Rules) bloquent l'accès ! Erreur : " + args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot != null && args.Snapshot.Exists)
        {
            Debug.Log("☁️ [ProfileManager] Dossier serveur trouvé ! Synchronisation vers le téléphone...");
            string jsonCloud = args.Snapshot.GetRawJsonValue();
            
            if (!string.IsNullOrEmpty(jsonCloud) && SaveManager.instance != null)
            {
                SaveManager.instance.EcraserAvecDonneesCloud(jsonCloud);
                RafraichirJeuComplet();
            }
        }
        else
        {
            Debug.Log("⚠️ [ProfileManager] Aucun dossier Firebase n'existe pour ce joueur. Création en cours...");
            if (SaveManager.instance != null)
            {
                string jsonLocal = JsonUtility.ToJson(SaveManager.instance.data);
                PousserSauvegardeVersCloud(jsonLocal);
            }
        }
    }

    public void PousserSauvegardeVersCloud(string jsonPartie)
    {
        if (dbReference == null || string.IsNullOrEmpty(uidJoueur)) return;

        bloqueEcouteTemporairement = true;

        dbReference.Child("Joueurs").Child(uidJoueur).Child("dataComplete").SetRawJsonValueAsync(jsonPartie).ContinueWithOnMainThread(task => 
        {
            bloqueEcouteTemporairement = false; 
            
            if (task.IsFaulted) 
            {
                Debug.LogError("🚨 [CRITIQUE DB] Échec de la création du dossier sur Firebase ! Vérifiez vos Règles. Erreur : " + task.Exception);
            }
            else 
            {
                Debug.Log("✅ [ProfileManager] Dossier créé et sauvegarde poussée sur le Cloud avec succès !");
            }
        });
        
        string pseudoActuel = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
        dbReference.Child("Joueurs").Child(uidJoueur).Child("pseudo").SetValueAsync(pseudoActuel);
    }

    private void RafraichirJeuComplet()
    {
        // Mise à jour locale (GameManager, etc.)
        if (GameManager.instance != null) 
        {
            GameManager.argentTotal = SaveManager.instance.data.argentTotal;
            GameManager.currentLevel = SaveManager.instance.data.niveau;
            GameManager.instance.MettreAJourUI();
        }
        
        if (ThemeManager.instance != null) 
        {
            ThemeManager.instance.RafraichirAffichageArgent();
            ThemeManager.instance.MettreAJourBoutonsBoutique();
        }

        UpgradeShopUI boutiqueUpgrade = FindObjectOfType<UpgradeShopUI>();
        if (boutiqueUpgrade != null && boutiqueUpgrade.gameObject.activeInHierarchy) boutiqueUpgrade.ActualiserBoutiqueUpgrades();

        // 🚀 NOUVEAU : On s'assure que le classement (Firestore) est forcé de s'aligner 
        // avec la sauvegarde que l'on vient de télécharger depuis le Cloud (RTDB).
        if (FirebaseManager.instance != null)
        {
            FirebaseManager.instance.SynchroniserFirestoreAvecDatabaseLocale();
            FirebaseManager.instance.RecupererClassement(); // Rafraîchit l'UI si elle est ouverte
        }
    }
}