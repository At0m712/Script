using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using Firebase.Analytics;
using Firebase.Auth; 
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

    private bool estEnModeSpeedrun = false;
    private int indexOngletSpeedrun = 0;

    private List<GameObject> poolDeLignes = new List<GameObject>();
    private int idRequeteActuelle = 0;

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

        foreach (Transform enfant in conteneurClassement)
        {
            if (maLigneFixeBas == null || enfant != maLigneFixeBas.transform)
            {
                Destroy(enfant.gameObject);
            }
        }

        if (!string.IsNullOrEmpty(GetUserId()))
        {
            RecupererClassement();
        }
    }

    private string GetUserId()
    {
        if (FirebaseAuth.DefaultInstance != null && FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            return FirebaseAuth.DefaultInstance.CurrentUser.UserId;
        }
        return null;
    }

    public void ActiverManagerApresConnexion(string uidIgnore)
    {
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        
        // 🚀 On télécharge le meilleur score de la database
        RestaurerDonneesDepuisCloud();

        // 🚀 NOUVEAU : On rattrape les anciens joueurs silencieusement
        RattraperAncienJoueur();
    }
    // =======================================================
    // 🚀 SYSTÈME DE COMPTEUR GLOBAL
    // =======================================================

    private void RattraperAncienJoueur()
    {
        // Si le joueur a DÉJÀ un pseudo (ancien joueur) MAIS qu'il n'a jamais été compté
        if (PlayerPrefs.HasKey("MonPseudoFirebase") && PlayerPrefs.GetInt("CompteurJoueurInscrit", 0) == 0)
        {
            DocumentReference statsRef = FirebaseFirestore.DefaultInstance.Collection("Statistiques").Document("Global");
            
            // On ajoute silencieusement +1 au compteur global
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "TotalJoueurs", FieldValue.Increment(1) }
            };
            
            statsRef.SetAsync(updates, SetOptions.MergeAll);

            // On le marque comme compté définitivement sur son appareil
            PlayerPrefs.SetInt("CompteurJoueurInscrit", 1);
            PlayerPrefs.Save();
            
            Debug.Log("Ancien joueur rattrapé et ajouté aux statistiques globales !");
        }
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
        SynchroniserFirestoreAvecDatabaseLocale();

        // 🚀 NOUVEAU : On ajoute +1 au compteur global de la base de données
        // On vérifie qu'on ne l'a pas déjà compté pour ne pas fausser les stats
        if (PlayerPrefs.GetInt("CompteurJoueurInscrit", 0) == 0)
        {
            DocumentReference statsRef = FirebaseFirestore.DefaultInstance.Collection("Statistiques").Document("Global");
            
            // FieldValue.Increment(1) est une fonction magique de Firebase 
            // qui fait "+1" de manière 100% sécurisée, même si 100 joueurs s'inscrivent à la même seconde !
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "TotalJoueurs", FieldValue.Increment(1) }
            };
            
            statsRef.SetAsync(updates, SetOptions.MergeAll);

            // On mémorise sur le téléphone que ce joueur a bien été compté
            PlayerPrefs.SetInt("CompteurJoueurInscrit", 1);
            PlayerPrefs.Save();
        }
    }
    // =======================================================
    // 🚀 GESTION DES SCORES (100% SÉCURISÉ)
    // =======================================================

    public void EnvoyerScore(int points)
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid) || SaveManager.instance == null) return;

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        int vraiMeilleurScore = SaveManager.instance.data.meilleurScore; 

        Dictionary<string, object> data = new Dictionary<string, object> {
            { "nom", nomJoueur }, 
            { "score", vraiMeilleurScore } 
        };

        FirebaseFirestore.DefaultInstance.Collection("ClassementClassique").Document(uid)
            .SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t => { 
                if (!estEnModeSpeedrun) RecupererClassement(); 
            });
    }

    public void EnvoyerTempsSpeedrun(float secondes, int indexNiveau)
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid) || SaveManager.instance == null) return;
        
        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        int vraiMeilleurTemps = SaveManager.instance.data.meilleursTempsSpeedrun[indexNiveau];
        
        if (vraiMeilleurTemps <= 0) return;

        Dictionary<string, object> data = new Dictionary<string, object> {
            { "nom", nomJoueur }, 
            { "temps_" + indexNiveau, vraiMeilleurTemps }
        };

        FirebaseFirestore.DefaultInstance.Collection("ClassementSpeedrun").Document(uid)
            .SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t => { 
                if (estEnModeSpeedrun && indexOngletSpeedrun == indexNiveau) RecupererClassement(); 
            });
    }

    // 🚀 NOUVEAU : Récupère le meilleur score de la database pour actualiser la sauvegarde locale
    public void RestaurerDonneesDepuisCloud()
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid) || SaveManager.instance == null) return;

        FirebaseFirestore.DefaultInstance.Collection("ClassementClassique").Document(uid).GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && task.Result.Exists)
            {
                Dictionary<string, object> data = task.Result.ToDictionary();
                if (data.ContainsKey("score"))
                {
                    int scoreDB = Convert.ToInt32(data["score"]);
                    
                    // Si la database a un meilleur score que la sauvegarde locale, la database gagne !
                    if (scoreDB > SaveManager.instance.data.meilleurScore)
                    {
                        SaveManager.instance.data.meilleurScore = scoreDB;
                        SaveManager.instance.SauvegarderPartie();
                        
                        if (GameManager.instance != null) GameManager.instance.MettreAJourUI();
                    }
                }
            }
            
            // Ensuite on synchronise dans le sens inverse (si le local avait de nouveaux temps speedrun)
            SynchroniserFirestoreAvecDatabaseLocale();
            RecupererClassement();
        });
    }

    public void SynchroniserFirestoreAvecDatabaseLocale()
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid) || SaveManager.instance == null) return;

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");

        int scoreDB = SaveManager.instance.data.meilleurScore;
        if (scoreDB > 0)
        {
            Dictionary<string, object> dataClassique = new Dictionary<string, object> {
                { "nom", nomJoueur }, { "score", scoreDB }
            };
            FirebaseFirestore.DefaultInstance.Collection("ClassementClassique").Document(uid).SetAsync(dataClassique, SetOptions.MergeAll);
        }

        Dictionary<string, object> dataSpeedrun = new Dictionary<string, object> { { "nom", nomJoueur } };
        bool aDesTempsAVerifier = false;

        for (int i = 0; i < SaveManager.instance.data.meilleursTempsSpeedrun.Count; i++)
        {
            int temps = SaveManager.instance.data.meilleursTempsSpeedrun[i];
            if (temps > 0)
            {
                dataSpeedrun["temps_" + i] = temps;
                aDesTempsAVerifier = true;
            }
        }

        if (aDesTempsAVerifier)
        {
            FirebaseFirestore.DefaultInstance.Collection("ClassementSpeedrun").Document(uid).SetAsync(dataSpeedrun, SetOptions.MergeAll);
        }
    }

    // =======================================================
    // 🚀 AFFICHAGE UI 100% CONNECTÉ À LA DATABASE
    // =======================================================

    public void RecupererClassement()
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid)) return;

        idRequeteActuelle++;
        int requeteEnCours = idRequeteActuelle;

        string collectionName = estEnModeSpeedrun ? "ClassementSpeedrun" : "ClassementClassique";
        string champTri = estEnModeSpeedrun ? "temps_" + indexOngletSpeedrun : "score";
        
        Query query = FirebaseFirestore.DefaultInstance.Collection(collectionName);
        
        if (estEnModeSpeedrun) 
            query = query.WhereGreaterThan(champTri, 0).OrderBy(champTri).Limit(50);
        else 
            query = query.OrderByDescending(champTri).Limit(50);

        query.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task => {
            
            if (requeteEnCours != idRequeteActuelle) return;
            if (task.IsFaulted || task.IsCanceled) return;
            if (this == null || conteneurClassement == null) return; 

            int rangActuel = 1;
            int monRang = -1;
            string monScoreTexte = "";
            int indexUI = 0;

            foreach (DocumentSnapshot document in task.Result.Documents)
            {
                Dictionary<string, object> data = document.ToDictionary();
                string nomAffiche = data.ContainsKey("nom") ? data["nom"].ToString() : "Joueur";
                bool cEstMoi = (document.Id == uid);
                string texteScore = "";
                
                if (estEnModeSpeedrun && data.ContainsKey(champTri)) 
                    texteScore = FormaterScoreEnChrono(Convert.ToInt64(data[champTri]));
                else if (!estEnModeSpeedrun && data.ContainsKey("score")) 
                    texteScore = data["score"].ToString() + " pts";

                if (cEstMoi) { monRang = rangActuel; monScoreTexte = texteScore; }

                GameObject ligneObj;
                if (indexUI < poolDeLignes.Count)
                {
                    ligneObj = poolDeLignes[indexUI];
                    ligneObj.SetActive(true);
                }
                else
                {
                    ligneObj = Instantiate(prefabLigneJoueur, conteneurClassement);
                    ligneObj.transform.localScale = Vector3.one; 
                    poolDeLignes.Add(ligneObj); 
                }

                ligneObj.GetComponent<LigneLeaderboard>().ConfigurerLigne(rangActuel, nomAffiche, texteScore, cEstMoi, false);
                
                rangActuel++;
                indexUI++;
            }

            for (int i = indexUI; i < poolDeLignes.Count; i++)
            {
                poolDeLignes[i].SetActive(false);
            }

            if (maLigneFixeBas != null)
            {
                string monNom = PlayerPrefs.GetString("MonPseudoFirebase", "Moi");
                
                // Si le joueur EST dans le top 50, on a déjà son score de la database
                if (monRang != -1) 
                {
                    maLigneFixeBas.ConfigurerLigne(monRang, monNom, monScoreTexte, true, true);
                }
                else 
                {
                    // 🚀 LA CORRECTION EST LÀ : S'il n'est pas dans le top 50, au lieu de lire sa sauvegarde locale,
                    // on interroge directement son document personnel dans Firebase !
                    FirebaseFirestore.DefaultInstance.Collection(collectionName).Document(uid).GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(taskMoi => 
                    {
                        if (taskMoi.IsCompleted && !taskMoi.IsFaulted && taskMoi.Result.Exists)
                        {
                            Dictionary<string, object> myData = taskMoi.Result.ToDictionary();
                            string dbTexteScore = "--";

                            if (estEnModeSpeedrun && myData.ContainsKey(champTri)) 
                                dbTexteScore = FormaterScoreEnChrono(Convert.ToInt64(myData[champTri]));
                            else if (!estEnModeSpeedrun && myData.ContainsKey("score")) 
                                dbTexteScore = myData["score"].ToString() + " pts";

                            maLigneFixeBas.ConfigurerLigne(0, monNom, dbTexteScore, true, true);
                        }
                        else
                        {
                            // S'il n'a pas encore de score dans la base de données
                            string texteVide = estEnModeSpeedrun ? "--:--.--" : "0 pts";
                            maLigneFixeBas.ConfigurerLigne(0, monNom, texteVide, true, true);
                        }
                    });
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