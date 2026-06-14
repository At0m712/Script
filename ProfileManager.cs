using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;

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
    private string idUniqueAppareil;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); 
            
            // 🛡️ SÉCURITÉ : On nettoie l'ID du téléphone des caractères interdits par Firebase (., #, $, [, ])
            idUniqueAppareil = SystemInfo.deviceUniqueIdentifier.Replace(".", "").Replace("#", "").Replace("$", "").Replace("[", "").Replace("]", "");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available) 
            {
                // 🛡️ SÉCURITÉ : On active le cache hors-ligne AVANT toute autre action !
                // Si le joueur ferme l'appli en cours de sauvegarde, Firebase terminera l'envoi au prochain lancement.
                FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(true);

                // Seulement ensuite, on crée la référence à la base
                dbReference = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").RootReference;
                InitialiserEtSynchroniser();
            } 
            else 
            {
                Debug.LogError("Erreur Firebase : " + task.Result);
            }
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

        dbReference.Child("Joueurs").Child(idUniqueAppareil).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            // 🚨 LE GILET DE SAUVETAGE POUR MOBILE : 
            // Si la lecture plante à cause du réseau 4G/Wifi instable, on force l'écriture quoiqu'il arrive !
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("Lecture Firebase bloquée, création du fichier forcée !");
                EnvoyerVersFirebase();
                StartCoroutine(TraqueurAutomatiqueDeChangements());
                return;
            }

            // Si tout s'est bien passé et que le fichier existe déjà
            if (task.IsCompleted && task.Result.Exists)
            {
                string jsonServeur = task.Result.GetRawJsonValue();
                ProfilJoueur profilCloud = JsonUtility.FromJson<ProfilJoueur>(jsonServeur);
                RestaurerSauvegardeSiBesoin(profilCloud);
            }
            else // S'il n'existe pas encore
            {
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
        if (dbReference != null)
        {
            string jsonProfil = JsonUtility.ToJson(monProfil);
            dbReference.Child("Joueurs").Child(idUniqueAppareil).SetRawJsonValueAsync(jsonProfil);
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

        // 2. CORRECTION : Restaurer et appliquer le Pseudo
        if (!string.IsNullOrEmpty(profilCloud.pseudo) && profilCloud.pseudo != "joueur")
        {
            PlayerPrefs.SetString("MonPseudoFirebase", profilCloud.pseudo);
            PlayerPrefs.Save();
            monProfil.pseudo = profilCloud.pseudo;

            // Transmettre immédiatement le nouveau pseudo aux classements Firestore
            if (FirebaseManager.instance != null) 
            {
                FirebaseManager.instance.DefinirPseudo(profilCloud.pseudo);
            }
        }

        SaveManager.instance.SauvegarderPartie();

        // 3. CORRECTION : Synchroniser le jeu en direct
        // Comme Firebase répond avec un léger retard, on force l'écrasement 
        // des valeurs déjà chargées par les autres scripts.
        if (GameManager.instance != null)
        {
            GameManager.argentTotal = profilCloud.nbPieces; // Met à jour le SafeInt anti-triche
            GameManager.instance.MettreAJourUI();           // Actualise l'interface en jeu
        }

        if (ThemeManager.instance != null)
        {
            ThemeManager.instance.RafraichirAffichageArgent(); // Actualise l'interface du menu/boutique
        }
    }
    // =================================================================
    // 🛡️ SÉCURITÉ ANDROID : SAUVEGARDE D'URGENCE
    // =================================================================
    
    void OnApplicationPause(bool isPaused)
    {
        // Dès que l'application passe en arrière-plan, on force l'envoi
        if (isPaused)
        {
            Debug.Log("📱 Android : Mise en pause détectée, sauvegarde d'urgence forcée !");
            
            // 1. On met à jour nos variables locales avant l'envoi
            if (SaveManager.instance != null)
            {
                monProfil.nbPieces = SaveManager.instance.data.argentTotal;
                monProfil.meilleurScoreClassique = SaveManager.instance.data.meilleurScore;
                monProfil.meilleurChronoSpeedrun = Mathf.FloorToInt(SaveManager.instance.data.meilleurTempsSpeedrun * 100f);
                monProfil.pseudo = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");
                
                // On force aussi la sauvegarde sur le téléphone
                SaveManager.instance.SauvegarderPartie();
            }

            // 2. On pousse immédiatement les données sur Firebase avant que le téléphone ne "tue" l'appli
            EnvoyerVersFirebase();
        }
    }
}