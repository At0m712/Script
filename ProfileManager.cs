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
        SaveManager.instance.data.argentTotal = profilCloud.nbPieces;
        SaveManager.instance.data.meilleurScore = profilCloud.meilleurScoreClassique;
        SaveManager.instance.data.meilleurTempsSpeedrun = profilCloud.meilleurChronoSpeedrun / 100f;
        
        monProfil.nbPieces = profilCloud.nbPieces;
        monProfil.meilleurScoreClassique = profilCloud.meilleurScoreClassique;
        monProfil.meilleurChronoSpeedrun = profilCloud.meilleurChronoSpeedrun;

        SaveManager.instance.SauvegarderPartie();
    }
}