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

    // 🚀 NOUVEAU : Le dossier qui contient nos 4 petits boutons
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

        // 🚀 NOUVEAU : Au démarrage, on est sur l'onglet Classique, donc on cache les boutons Speedrun
        if (conteneurBoutonsNiveaux != null)
        {
            conteneurBoutonsNiveaux.SetActive(false);
        }
    }

    public void ActiverManagerApresConnexion(string uid)
    {
        userId = uid;
        db = FirebaseFirestore.DefaultInstance;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        estConnecte = true;
        
        Debug.Log("✅ [FirebaseManager] Base de données Firestore prête pour le joueur : " + userId);
        RecupererClassement();
    }

    // 🚀 NOUVEAU : C'est ici que la magie s'opère quand on clique sur les gros onglets
    public void ChangerOnglet(bool versSpeedrun)
    {
        estEnModeSpeedrun = versSpeedrun;

        // On affiche les 4 boutons SI on va vers Speedrun, sinon on les cache
        if (conteneurBoutonsNiveaux != null)
        {
            conteneurBoutonsNiveaux.SetActive(versSpeedrun);
        }

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
        
        DocumentReference docClassique = db.Collection("ClassementClassique").Document(userId);
        docClassique.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.Result.Exists) docClassique.UpdateAsync(userData);
            else docClassique.SetAsync(userData);
        });

        for (int i = 0; i < 4; i++)
        {
            DocumentReference docSpeedrun = db.Collection("ClassementSpeedrun_" + i).Document(userId);
            docSpeedrun.GetSnapshotAsync().ContinueWithOnMainThread(task => {
                if (task.Result.Exists) docSpeedrun.UpdateAsync(userData);
            });
        }
    }

    public void EnvoyerScore(int points)
    {
        if (!estConnecte || string.IsNullOrEmpty(userId)) return;

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        DocumentReference docRef = db.Collection("ClassementClassique").Document(userId);
        
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) return;

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur }, { "score", points }
            };

            if (snapshot.Exists)
            {
                long ancienScore = 0;
                var dict = snapshot.ToDictionary();
                if (dict.ContainsKey("score")) ancienScore = Convert.ToInt64(dict["score"]);

                if (points > ancienScore) 
                {
                    docRef.UpdateAsync(data).ContinueWithOnMainThread(t => { RecupererClassement(); });
                }
            }
            else
            {
                docRef.SetAsync(data).ContinueWithOnMainThread(t => { RecupererClassement(); });
            }
        });
    }

    public void EnvoyerTempsSpeedrun(float secondes, int indexNiveau)
    {
        if (!estConnecte || string.IsNullOrEmpty(userId)) return;
        
        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        long tempsEnCentiemes = Mathf.FloorToInt(secondes * 100f);
        
        DocumentReference docRef = db.Collection("ClassementSpeedrun_" + indexNiveau).Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) return;

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur }, { "temps", tempsEnCentiemes }
            };

            if (snapshot.Exists)
            {
                long ancienTemps = long.MaxValue; 
                var dict = snapshot.ToDictionary();
                if (dict.ContainsKey("temps")) ancienTemps = Convert.ToInt64(dict["temps"]);

                if (tempsEnCentiemes < ancienTemps) 
                {
                    docRef.UpdateAsync(data).ContinueWithOnMainThread(t => { RecupererClassement(); });
                }
            }
            else
            {
                docRef.SetAsync(data).ContinueWithOnMainThread(t => { RecupererClassement(); });
            }
        });
    }

    public void RecupererClassement()
    {
        if (!estConnecte || db == null) return;

        string collectionName = estEnModeSpeedrun ? "ClassementSpeedrun_" + indexOngletSpeedrun : "ClassementClassique";
        string champTri = estEnModeSpeedrun ? "temps" : "score";
        
        Query query = db.Collection(collectionName);
        if (estEnModeSpeedrun) query = query.OrderBy(champTri).Limit(50);
        else query = query.OrderByDescending(champTri).Limit(50);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) return;

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
                
                if (estEnModeSpeedrun && data.ContainsKey("temps")) texteScore = FormaterScoreEnChrono(Convert.ToInt64(data["temps"]));
                else if (!estEnModeSpeedrun && data.ContainsKey("score")) texteScore = data["score"].ToString() + " pts";

                if (cEstMoi) { monRang = rangActuel; monScoreTexte = texteScore; }

                GameObject nouvelleLigne = Instantiate(prefabLigneJoueur, conteneurClassement);
                nouvelleLigne.transform.localScale = Vector3.one; 
                nouvelleLigne.GetComponent<LigneLeaderboard>().ConfigurerLigne(rangActuel, nomAffiche, texteScore, cEstMoi, false);
                rangActuel++;
            }

            if (maLigneFixeBas != null)
            {
                string monNom = PlayerPrefs.GetString("MonPseudoFirebase", "Moi");
                if (monRang != -1) maLigneFixeBas.ConfigurerLigne(monRang, monNom, monScoreTexte, true, true);
                else maLigneFixeBas.ConfigurerLigne(0, monNom, "Non classé", true, true);
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