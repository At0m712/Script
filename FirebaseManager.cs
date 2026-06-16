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

    // 🚀 OPTIMISATION : Système anti-lag et anti-doublons
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

        // 🧹 NETTOYAGE DE SÉCURITÉ : On supprime les faux éléments laissés dans l'éditeur
        foreach (Transform enfant in conteneurClassement)
        {
            // On ne détruit surtout pas ta ligne fixe si jamais elle est dans le même dossier
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
        SynchroniserFirestoreAvecDatabaseLocale();
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
        SynchroniserFirestoreAvecDatabaseLocale();
    }

    // =======================================================
    // 🚀 SYSTÈME D'ENVOI OPTIMISÉ (Base Locale = Maître)
    // =======================================================

    public void EnvoyerScore(int points)
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid) || SaveManager.instance == null) return;

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        int vraiMeilleurScore = SaveManager.instance.data.meilleurScore; // On force l'usage de la base locale

        Dictionary<string, object> data = new Dictionary<string, object> {
            { "nom", nomJoueur }, 
            { "score", vraiMeilleurScore } 
        };

        FirebaseFirestore.DefaultInstance.Collection("ClassementClassique").Document(uid)
            .SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(t => { 
                RecupererClassement(); 
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
    // 🚀 AFFICHAGE UI OPTIMISÉ ET SÉCURISÉ
    // =======================================================

    public void RecupererClassement()
    {
        string uid = GetUserId();
        if (string.IsNullOrEmpty(uid)) return;

        // 🛡️ SÉCURITÉ ANTI-DOUBLONS : On crée un ID de requête unique
        idRequeteActuelle++;
        int requeteEnCours = idRequeteActuelle;

        string collectionName = estEnModeSpeedrun ? "ClassementSpeedrun" : "ClassementClassique";
        string champTri = estEnModeSpeedrun ? "temps_" + indexOngletSpeedrun : "score";
        
        Query query = FirebaseFirestore.DefaultInstance.Collection(collectionName);
        
        if (estEnModeSpeedrun) 
            query = query.WhereGreaterThan(champTri, 0).OrderBy(champTri).Limit(50);
        else 
            query = query.OrderByDescending(champTri).Limit(50);

        // Source.Server oblige l'application à lire les VRAIES données, jamais le cache local de l'appareil
        query.GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(task => {
            
            // 🛑 LA MAGIE EST ICI : Si une autre requête a été lancée entre temps (parce que le joueur a cliqué vite), on annule celle-ci direct !
            if (requeteEnCours != idRequeteActuelle) return;

            if (task.IsFaulted || task.IsCanceled) 
            {
                Debug.LogError("🚨 [Firebase] Erreur chargement Leaderboard : " + task.Exception);
                return;
            }

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

                // 🚀 GESTION PROPRE DU POOLING (Zéro doublon, zéro lag)
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
                    poolDeLignes.Add(ligneObj); // On l'ajoute à notre liste sécurisée
                }

                ligneObj.GetComponent<LigneLeaderboard>().ConfigurerLigne(rangActuel, nomAffiche, texteScore, cEstMoi, false);
                
                rangActuel++;
                indexUI++;
            }

            // 🧹 On éteint proprement TOUTES les lignes en trop (fini les scores fantômes !)
            for (int i = indexUI; i < poolDeLignes.Count; i++)
            {
                poolDeLignes[i].SetActive(false);
            }

            // Affichage de ma propre ligne fixée en bas
            if (maLigneFixeBas != null)
            {
                string monNom = PlayerPrefs.GetString("MonPseudoFirebase", "Moi");
                if (monRang != -1) 
                {
                    maLigneFixeBas.ConfigurerLigne(monRang, monNom, monScoreTexte, true, true);
                }
                else 
                {
                    string monScoreLocal = "";
                    if (estEnModeSpeedrun)
                    {
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