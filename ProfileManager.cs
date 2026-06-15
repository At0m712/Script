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
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                dbReference = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                auth = FirebaseAuth.DefaultInstance;

                if (GooglePlayManager.instance != null) GooglePlayManager.instance.LancerConnexionGoogleEtFirebase(auth);
                else ConnecterAnonymement();
            } 
            else 
            {
                Debug.LogError("🚨 Erreur Firebase : " + task.Result);
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

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task => {
            if (!task.IsCanceled && !task.IsFaulted) DemarrerSynchronisation(task.Result.User.UserId);
        });
    }

    public void DemarrerSynchronisation(string uid)
    {
        uidJoueur = uid;
        Debug.Log("📍 [ProfileManager] Lancement de l'écoute Serveur pour l'UID : " + uidJoueur);

        // 🔗 NOUVEAU : On donne le signal au FirebaseManager (Leaderboards) qu'il peut s'activer !
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

        if (args.Snapshot.Exists)
        {
            string jsonCloud = args.Snapshot.GetRawJsonValue();
            
            if (!string.IsNullOrEmpty(jsonCloud) && SaveManager.instance != null)
            {
                SaveManager.instance.EcraserAvecDonneesCloud(jsonCloud);
                RafraichirJeuComplet();
            }
        }
        else
        {
            // CRÉATION DU DOSSIER : Si le joueur n'a pas de dossier, on le crée en envoyant la save locale
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
            if (task.IsFaulted) Debug.LogError("🚨 Erreur d'écriture Serveur : " + task.Exception);
            else Debug.Log("✅ [ProfileManager] Sauvegarde poussée sur le Cloud et Dossier créé !");
        });
        
        string pseudoActuel = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
        dbReference.Child("Joueurs").Child(uidJoueur).Child("pseudo").SetValueAsync(pseudoActuel);
    }

    private void RafraichirJeuComplet()
    {
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
    }
}