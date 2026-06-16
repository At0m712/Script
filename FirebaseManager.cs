using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using Firebase.Analytics;
using System; 

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;

    [Header("Interface (Génération par Prefab)")]
    public GameObject prefabLigneJoueur; 
    public Transform conteneurClassement; 
    public LigneLeaderboard maLigneFixeBas; 
    public GameObject panelSaisiePseudo;

    [Header("Interface Niveaux Speedrun")]
    public GameObject conteneurBoutonsNiveaux; 

    private FirebaseFirestore db;
    private string userId;
    
    private bool estEnModeSpeedrun = false;
    private bool estConnecte = false; 

    private int indexOngletSpeedrun = 0;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (panelSaisiePseudo != null)
        {
            panelSaisiePseudo.SetActive(!PlayerPrefs.HasKey("MonPseudoFirebase"));
        }

        if (conteneurBoutonsNiveaux != null) conteneurBoutonsNiveaux.SetActive(false);
    }

    public void ActiverManagerApresConnexion(string uid)
    {
        userId = uid;
        db = FirebaseFirestore.DefaultInstance;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        estConnecte = true;
        
        Debug.Log("✅ [FirebaseManager] Firestore prêt ! Chargement du classement (Serveur)...");
        RecupererClassement();
    }

    public void AfficherClassementClassique()
    {
        estEnModeSpeedrun = false;
        if (conteneurBoutonsNiveaux != null) conteneurBoutonsNiveaux.SetActive(false);
        RecupererClassement();
    }

    public void AfficherClassementSpeedrun()
    {
        estEnModeSpeedrun = true;
        indexOngletSpeedrun = 0; 
        if (conteneurBoutonsNiveaux != null) conteneurBoutonsNiveaux.SetActive(true);
        RecupererClassement();
    }

    public void ChangerNiveauSpeedrunLeaderboard(int index)
    {
        indexOngletSpeedrun = index;
        if (estEnModeSpeedrun) RecupererClassement(); 
    }

    public void DefinirPseudo(string pseudoJoueur)
    {
        PlayerPrefs.SetString("MonPseudoFirebase", pseudoJoueur);
        PlayerPrefs.Save();
        
        if (!estConnecte || string.IsNullOrEmpty(userId)) return;

        Dictionary<string, object> userData = new Dictionary<string, object> { { "nom", pseudoJoueur } };
        
        // On utilise SetOptions.MergeAll pour ne jamais effacer les autres données du joueur
        db.Collection("ClassementClassique").Document(userId).SetAsync(userData, SetOptions.MergeAll);
        db.Collection("ClassementSpeedrun").Document(userId).SetAsync(userData, SetOptions.MergeAll);
    }

    public void EnvoyerScore(int points)
    {
        if (!estConnecte || string.IsNullOrEmpty(userId)) return;

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        DocumentReference docRef = db.Collection("ClassementClassique").Document(userId);
        
        // 🚀 FORCE SERVER : On ignore le cache pour éviter les bugs de lecture
        docRef.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) return;

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur }, { "score", points }
            };

            long ancienScore = 0;
            if (snapshot.Exists)
            {
                var dict = snapshot.ToDictionary();
                if (dict.ContainsKey("score")) ancienScore = Convert.ToInt64(dict["score"]);
            }

            if (points > ancienScore) 
            {
                // 🚀 MERGE ALL : Met à jour ou crée sans effacer le reste
                docRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t => { RecupererClassement(); });
            }
        });
    }

    public void EnvoyerTempsSpeedrun(float secondes, int indexNiveau)
    {
        if (!estConnecte || string.IsNullOrEmpty(userId)) return;
        
        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        long tempsEnCentiemes = Mathf.FloorToInt(secondes * 100f);
        
        DocumentReference docRef = db.Collection("ClassementSpeedrun").Document(userId);
        string nomDuChampTemps = "temps_" + indexNiveau;

        // 🚀 FORCE SERVER
        docRef.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task => {
            if (task.IsFaulted) return;

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur }, 
                { nomDuChampTemps, tempsEnCentiemes }
            };

            long ancienTemps = long.MaxValue; 

            if (snapshot.Exists)
            {
                var dict = snapshot.ToDictionary();
                if (dict.ContainsKey(nomDuChampTemps)) ancienTemps = Convert.ToInt64(dict[nomDuChampTemps]);
            }

            // 🚀 CORRECTION : Si le joueur a un vieux temps buggé à 0, on l'écrase obligatoirement
            if (tempsEnCentiemes < ancienTemps || ancienTemps <= 0) 
            {
                // 🚀 MERGE ALL : Ajoute le temps de ce niveau SANS effacer les autres niveaux existants !
                docRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t => { RecupererClassement(); });
            }
        });
    }

    public void RecupererClassement()
    {
        if (!estConnecte || db == null) return;

        string collectionName = estEnModeSpeedrun ? "ClassementSpeedrun" : "ClassementClassique";
        string champTri = estEnModeSpeedrun ? "temps_" + indexOngletSpeedrun : "score";
        
        Query query = db.Collection(collectionName);
        
        if (estEnModeSpeedrun) 
        {
            // 🚀 FILTRE MAGIQUE : Ignore automatiquement tous les scores buggés qui sont à 0
            query = query.WhereGreaterThan(champTri, 0).OrderBy(champTri).Limit(50);
        }
        else 
        {
            query = query.OrderByDescending(champTri).Limit(50);
        }

        // 🚀 FORCE SERVER : Lis la vraie base de données, règle le problème des scores fantômes !
        query.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) 
            {
                Debug.LogError("🚨 [Firebase] Erreur chargement Leaderboard : " + task.Exception);
                return;
            }

            foreach (Transform enfant in conteneurClassement) Destroy(enfant.gameObject);

            int rangActuel = 1;
            int monRang = -1;
            string monScoreTexte = "";

            foreach (DocumentSnapshot document in task.Result.Documents)
            {
                Dictionary<string, object> data = document.ToDictionary();
                
                string nomAffiche = data.ContainsKey("nom") ? data["nom"].ToString() : "Joueur";
                bool cEstMoi = (document.Id == userId);
                string texteScore = "";
                
                if (estEnModeSpeedrun && data.ContainsKey(champTri)) 
                    texteScore = FormaterScoreEnChrono(Convert.ToInt64(data[champTri]));
                else if (!estEnModeSpeedrun && data.ContainsKey("score")) 
                    texteScore = data["score"].ToString() + " pts";

                if (cEstMoi) { monRang = rangActuel; monScoreTexte = texteScore; }

                GameObject nouvelleLigne = Instantiate(prefabLigneJoueur, conteneurClassement);
                nouvelleLigne.transform.localScale = Vector3.one; 
                nouvelleLigne.GetComponent<LigneLeaderboard>().ConfigurerLigne(rangActuel, nomAffiche, texteScore, cEstMoi, false);
                rangActuel++;
            }

            if (maLigneFixeBas != null)
            {
                string monNom = PlayerPrefs.GetString("MonPseudoFirebase", "Moi");
                if (monRang != -1) 
                {
                    // Si on est dans le Top 50, on affiche notre rang officiel
                    maLigneFixeBas.ConfigurerLigne(monRang, monNom, monScoreTexte, true, true);
                }
                else 
                {
                    // 🚀 NOUVEAU : Si on est pas dans le Top 50, on affiche notre VRAI score local !
                    string monScoreLocal = "";
                    
                    if (estEnModeSpeedrun)
                    {
                        // 👉 CORRECTION : tempsLocal est un 'int' maintenant
                        int tempsLocal = SaveManager.instance.data.meilleursTempsSpeedrun[indexOngletSpeedrun];
                        
                        if (tempsLocal > 0) monScoreLocal = FormaterScoreEnChrono(tempsLocal);
                        else monScoreLocal = "--:--.--"; 
                    }
                    else
                    {
                        monScoreLocal = SaveManager.instance.data.meilleurScore + " pts";
                    }
                    
                    maLigneFixeBas.ConfigurerLigne(0, monNom, monScoreLocal, true, true);
                }
            }
        });
    }

    private string FormaterScoreEnChrono(long scoreCentiemes)
    {
        float tempsTotal = scoreCentiemes / 100f;
        int minutes = Mathf.FloorToInt(tempsTotal / 60f);
        int secondes = Mathf.FloorToInt(tempsTotal % 60f);
        int centiemes = Mathf.FloorToInt((tempsTotal * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, secondes, centiemes);
    }
    
    public void AnalyserMortJoueur(int niveau, int score)
    {
        FirebaseAnalytics.LogEvent("joueur_est_mort", new Parameter("niveau_atteint", niveau), new Parameter("score_final", score));
    }

    public void AnalyserAchat(string nomObjet, int prix)
    {
        FirebaseAnalytics.LogEvent("achat_boutique", new Parameter("nom_objet", nomObjet), new Parameter("prix_objet", prix));
    }
}