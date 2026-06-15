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
            
            // 🛡️ SÉCURITÉ ANDROID : On récupère l'ID natif de l'appareil
            string idBrut = SystemInfo.deviceUniqueIdentifier;

            // 🛡️ Si l'ID est introuvable sur ce téléphone ou vide, on génère un identifiant de secours !
            if (string.IsNullOrEmpty(idBrut) || idBrut == SystemInfo.unsupportedIdentifier)
            {
                if (PlayerPrefs.HasKey("ID_Secours_Appareil"))
                {
                    idBrut = PlayerPrefs.GetString("ID_Secours_Appareil");
                }
                else
                {
                    idBrut = System.Guid.NewGuid().ToString(); // Crée un ID unique aléatoire
                    PlayerPrefs.SetString("ID_Secours_Appareil", idBrut);
                    PlayerPrefs.Save();
                }
            }

            // 🛡️ NETTOYAGE EXTRÊME : Firebase interdit : . # $ [ ] et les slashs / \
            idUniqueAppareil = idBrut
                .Replace(".", "")
                .Replace("#", "")
                .Replace("$", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("/", "")
                .Replace("\\", "")
                .Trim();
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
                
                Debug.Log("📍 [ProfileManager] 3. dbReference créée. Attente de l'Authentification...");
                
                // 🔴 CORRECTION CRITIQUE : On appelle la Coroutine au lieu de synchroniser directement !
                // Cela va écraser le faux ID (SystemInfo) par le VRAI ID Firebase.
                StartCoroutine(AttendreAuthentification());
            } 
            else 
            {
                Debug.LogError("🚨 [ProfileManager] Erreur Firebase : " + task.Result);
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

        Debug.Log("📍 [ProfileManager] 4. Envoi de la requête de lecture au serveur pour l'ID : " + idUniqueAppareil);

        dbReference.Child("Joueurs").Child(idUniqueAppareil).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            Debug.Log("📍 [ProfileManager] 5. Réponse du serveur reçue !");

            // 🚨 SÉCURITÉ : Gestion des erreurs (Règles expirées, coupure internet...)
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning("⚠️ Lecture Firebase bloquée (IsFaulted). Création forcée ! Détail : " + task.Exception);
                EnvoyerVersFirebase();
                StartCoroutine(TraqueurAutomatiqueDeChangements());
                return;
            }

            if (task.IsCompleted && task.Result.Exists)
            {
                Debug.Log("📍 [ProfileManager] 6. Profil existant trouvé sur le cloud.");
                ProfilJoueur profilCloud = new ProfilJoueur();
                
                try
                {
                    string jsonServeur = task.Result.GetRawJsonValue();
                    ProfilJoueur tempProfil = JsonUtility.FromJson<ProfilJoueur>(jsonServeur);
                    if (tempProfil != null) profilCloud = tempProfil;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Format JSON invalide sur Firebase, récupération manuelle... Erreur : " + e.Message);
                    if (task.Result.HasChild("nbPieces")) profilCloud.nbPieces = int.Parse(task.Result.Child("nbPieces").Value.ToString());
                    if (task.Result.HasChild("meilleurScoreClassique")) profilCloud.meilleurScoreClassique = int.Parse(task.Result.Child("meilleurScoreClassique").Value.ToString());
                    if (task.Result.HasChild("meilleurChronoSpeedrun")) profilCloud.meilleurChronoSpeedrun = int.Parse(task.Result.Child("meilleurChronoSpeedrun").Value.ToString());
                    if (task.Result.HasChild("pseudo")) profilCloud.pseudo = task.Result.Child("pseudo").Value.ToString();
                }

                RestaurerSauvegardeSiBesoin(profilCloud);
            }
            else // S'il n'existe pas encore
            {
                Debug.Log("📍 [ProfileManager] 6. Profil introuvable. Création d'un nouveau profil !");
                EnvoyerVersFirebase();
            }

            StartCoroutine(TraqueurAutomatiqueDeChangements());
        });
    }

    private IEnumerator AttendreAuthentification()
    {
        // On boucle tant que le FirebaseManager n'a pas enregistré le vrai ID du joueur
        while (!PlayerPrefs.HasKey("MonIDFirebase"))
        {
            yield return new WaitForSeconds(0.2f);
        }

        // On récupère le VRAI ID Firebase (le même que pour les classements !)
        idUniqueAppareil = PlayerPrefs.GetString("MonIDFirebase");
        
        Debug.Log("📂 ProfileManager lié à l'ID officiel : " + idUniqueAppareil);
        
        // Maintenant qu'on a le bon ID, on lance la création/téléchargement du dossier !
        InitialiserEtSynchroniser();
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
        // On empêche le crash silencieux si l'ID a échoué
        if (dbReference != null && !string.IsNullOrEmpty(idUniqueAppareil))
        {
            string jsonProfil = JsonUtility.ToJson(monProfil);
            
            // On ajoute le '.ContinueWithOnMainThread' pour sécuriser l'envoi depuis le mobile
            dbReference.Child("Joueurs").Child(idUniqueAppareil).SetRawJsonValueAsync(jsonProfil).ContinueWithOnMainThread(task => 
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("🚨 ERREUR FIREBASE ANDROID (Création Dossier bloquée) : " + task.Exception.ToString());
                }
                else
                {
                    Debug.Log("✅ Dossier Joueur créé ou mis à jour avec succès sur mobile !");
                }
            });
        }
        else
        {
            Debug.LogWarning("⚠️ Annulation de l'envoi : Firebase non prêt ou ID invalide.");
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