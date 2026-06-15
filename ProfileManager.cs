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
    
    // Sécurité pour éviter une boucle infinie d'écoute quand c'est NOUS qui sauvegardons
    private bool bloqueEcouteTemporairement = false; 

    void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        Debug.Log("📍 [ProfileManager] Initialisation de Firebase...");
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available) 
            {
                // OPTIMISATION : Active le cache hors-ligne natif de Firebase
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);
                
                dbReference = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                auth = FirebaseAuth.DefaultInstance;

                // On lance la connexion via Google Play (Qui nous ramènera ici via DemarrerSynchronisation)
                if (GooglePlayManager.instance != null)
                {
                    GooglePlayManager.instance.LancerConnexionGoogleEtFirebase(auth);
                }
                else
                {
                    ConnecterAnonymement();
                }
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
            if (!task.IsCanceled && !task.IsFaulted)
            {
                DemarrerSynchronisation(task.Result.User.UserId);
            }
        });
    }

    public void DemarrerSynchronisation(string uid)
    {
        uidJoueur = uid;
        Debug.Log("📍 [ProfileManager] Lancement de l'écoute Serveur pour l'UID : " + uidJoueur);

        // LE CŒUR DU SYSTÈME : On s'abonne aux changements de la base de données en DIRECT !
        DatabaseReference dataRef = dbReference.Child("Joueurs").Child(uidJoueur).Child("dataComplete");
        dataRef.ValueChanged += SurChangementServeur;
    }

    // ==========================================
    // ⬇️ REÇOIT LES DONNÉES DU SERVEUR EN DIRECT
    // ==========================================
    private void SurChangementServeur(object sender, ValueChangedEventArgs args)
    {
        if (bloqueEcouteTemporairement) return; // Si c'est nous qui venons d'écrire, on s'ignore

        if (args.Snapshot.Exists)
        {
            string jsonCloud = args.Snapshot.GetRawJsonValue();
            
            if (!string.IsNullOrEmpty(jsonCloud) && SaveManager.instance != null)
            {
                // 1. On écrase la sauvegarde locale
                SaveManager.instance.EcraserAvecDonneesCloud(jsonCloud);
                
                // 2. On actualise tout le jeu visuellement pour que le joueur voie le changement
                RafraichirJeuComplet();
            }
        }
        else
        {
            // C'est un nouveau joueur sur Firebase, on lui pousse notre sauvegarde locale initiale
            if (SaveManager.instance != null)
            {
                string jsonLocal = JsonUtility.ToJson(SaveManager.instance.data);
                PousserSauvegardeVersCloud(jsonLocal);
            }
        }
    }

    // ==========================================
    // ⬆️ ENVOIE LES DONNÉES AU SERVEUR (Appelé par SaveManager)
    // ==========================================
    public void PousserSauvegardeVersCloud(string jsonPartie)
    {
        if (dbReference == null || string.IsNullOrEmpty(uidJoueur)) return;

        // On bloque l'écouteur le temps d'écrire pour ne pas déclencher un aller-retour inutile
        bloqueEcouteTemporairement = true;

        dbReference.Child("Joueurs").Child(uidJoueur).Child("dataComplete").SetRawJsonValueAsync(jsonPartie).ContinueWithOnMainThread(task => 
        {
            bloqueEcouteTemporairement = false; // On rouvre les écoutes
            
            if (task.IsFaulted) Debug.LogError("🚨 Erreur d'écriture Serveur : " + task.Exception);
            else Debug.Log("✅ [ProfileManager] Sauvegarde poussée sur le Cloud !");
        });
        
        // On met aussi le pseudo en clair dans le dossier pour le lire facilement
        string pseudoActuel = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
        dbReference.Child("Joueurs").Child(uidJoueur).Child("pseudo").SetValueAsync(pseudoActuel);
    }

    // Met à jour toutes les UI du jeu si une modification serveur "pop" en direct
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

        // Si tu as un script Boutique Upgrade, il doit se rafraichir aussi
        UpgradeShopUI boutiqueUpgrade = FindObjectOfType<UpgradeShopUI>();
        if (boutiqueUpgrade != null && boutiqueUpgrade.gameObject.activeInHierarchy)
        {
            boutiqueUpgrade.ActualiserBoutiqueUpgrades();
        }
    }
}