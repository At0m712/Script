using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using Firebase.Analytics;
using System; // ✅ Indispensable pour sécuriser les conversions de nombres

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager instance;

    [Header("Interface (Génération par Prefab)")]
    public GameObject prefabLigneJoueur; 
    public Transform conteneurClassement; 
    public LigneLeaderboard maLigneFixeBas; 
    public GameObject panelSaisiePseudo;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string userId;
    
    private bool estEnModeSpeedrun = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        Debug.Log("Initialisation de Firebase...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Debug.Log("✅ Firebase est prêt !");
                SeConnecterAnonymement();
            }
            else
            {
                Debug.LogError("🚨 Impossible de résoudre les dépendances Firebase : " + task.Result);
            }
        });

        if (panelSaisiePseudo != null)
        {
            // ✅ CORRECTION : On vérifie la présence de la clé officielle !
            panelSaisiePseudo.SetActive(!PlayerPrefs.HasKey("MonPseudoFirebase"));
        }
    }

    void SeConnecterAnonymement()
    {
        Debug.Log("Tentative de connexion à Firebase Auth...");
        
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 ERREUR DE CONNEXION : " + task.Exception);
                return;
            }

            if (PlayerPrefs.HasKey("MonIDFirebase"))
            {
                userId = PlayerPrefs.GetString("MonIDFirebase");
                Debug.Log("🔄 Ancien compte retrouvé en mémoire : " + userId);
            }
            else
            {
                userId = task.Result.User.UserId;
                PlayerPrefs.SetString("MonIDFirebase", userId);
                PlayerPrefs.Save();
                Debug.Log("✅ Nouveau compte créé avec l'ID : " + userId);
            }
            
            RecupererClassement();
        });
    }

    public void ChangerOnglet(bool versSpeedrun)
    {
        estEnModeSpeedrun = versSpeedrun;
        RecupererClassement();
    }

    public void DefinirPseudo(string pseudoJoueur)
    {
        PlayerPrefs.SetString("MonPseudoFirebase", pseudoJoueur);
        PlayerPrefs.Save();
        
        if (string.IsNullOrEmpty(userId) || db == null) return;

        Dictionary<string, object> userData = new Dictionary<string, object> { { "nom", pseudoJoueur } };
        
        DocumentReference docClassique = db.Collection("ClassementClassique").Document(userId);
        docClassique.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { Debug.LogError("🚨 Erreur lecture pseudo Classique : " + task.Exception); return; }
            if (task.Result.Exists) docClassique.UpdateAsync(userData);
            else docClassique.SetAsync(userData);
        });

        DocumentReference docSpeedrun = db.Collection("ClassementSpeedrun").Document(userId);
        docSpeedrun.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) { Debug.LogError("🚨 Erreur lecture pseudo Speedrun : " + task.Exception); return; }
            if (task.Result.Exists) docSpeedrun.UpdateAsync(userData);
            else docSpeedrun.SetAsync(userData);
        });
    }

    public void EnvoyerScore(int points)
    {
        if (string.IsNullOrEmpty(userId) || db == null) 
        {
            Debug.LogError("🚨 ERREUR : Impossible d'envoyer le score, Firebase n'est pas connecté.");
            return;
        }

        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        DocumentReference docRef = db.Collection("ClassementClassique").Document(userId);
        
        Debug.Log("Vérification du score actuel sur le serveur...");

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) 
            {
                Debug.LogError("🚨 ERREUR lors de la vérification du score : " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur },
                { "score", points }
            };

            if (snapshot.Exists)
            {
                long ancienScore = 0;
                var dict = snapshot.ToDictionary();
                
                // ✅ CORRECTION 1 : Convert évite le crash silencieux si Firebase renvoie un int au lieu d'un long
                if (dict.ContainsKey("score")) ancienScore = Convert.ToInt64(dict["score"]);

                if (points > ancienScore) 
                {
                    docRef.UpdateAsync(data).ContinueWithOnMainThread(t => {
                        if (t.IsFaulted) Debug.LogError("🚨 ERREUR MISE À JOUR : " + t.Exception);
                        else 
                        {
                            Debug.Log("✅ Score mis à jour sur le serveur !");
                            RecupererClassement(); // ✅ CORRECTION 2 : Rafraîchir l'UI
                        }
                    });
                }
                else
                {
                    Debug.Log("Le nouveau score (" + points + ") n'est pas meilleur que l'ancien (" + ancienScore + "). On ne sauvegarde pas.");
                }
            }
            else
            {
                docRef.SetAsync(data).ContinueWithOnMainThread(t => {
                    if (t.IsFaulted) Debug.LogError("🚨 ERREUR CRÉATION SCORE : " + t.Exception);
                    else 
                    {
                        Debug.Log("✅ Nouveau profil de score créé sur le serveur !");
                        RecupererClassement(); // ✅ CORRECTION 2 : Rafraîchir l'UI
                    }
                });
            }
        });
    }

    public void EnvoyerTempsSpeedrun(float secondes)
    {
        if (string.IsNullOrEmpty(userId) || db == null) return;
        
        string nomJoueur = PlayerPrefs.GetString("MonPseudoFirebase", "Joueur");
        long tempsEnCentiemes = Mathf.FloorToInt(secondes * 100f);

        DocumentReference docRef = db.Collection("ClassementSpeedrun").Document(userId);

        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted) 
            {
                Debug.LogError("🚨 ERREUR lors de la vérification du chrono : " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            Dictionary<string, object> data = new Dictionary<string, object> {
                { "nom", nomJoueur },
                { "temps", tempsEnCentiemes }
            };

            if (snapshot.Exists)
            {
                long ancienTemps = long.MaxValue; 
                var dict = snapshot.ToDictionary();
                
                // ✅ CORRECTION 1 : Sécurisation du cast
                if (dict.ContainsKey("temps")) ancienTemps = Convert.ToInt64(dict["temps"]);

                if (tempsEnCentiemes < ancienTemps) 
                {
                    docRef.UpdateAsync(data).ContinueWithOnMainThread(t => {
                        if (t.IsFaulted) Debug.LogError("🚨 ERREUR MISE À JOUR TEMPS : " + t.Exception);
                        else 
                        {
                            Debug.Log("✅ Chrono record mis à jour !");
                            RecupererClassement(); // ✅ CORRECTION 2 : Rafraîchir l'UI
                        }
                    });
                }
            }
            else
            {
                docRef.SetAsync(data).ContinueWithOnMainThread(t => {
                    if (t.IsFaulted) Debug.LogError("🚨 ERREUR CRÉATION TEMPS : " + t.Exception);
                    else 
                    {
                        Debug.Log("✅ Premier chrono enregistré !");
                        RecupererClassement(); // ✅ CORRECTION 2 : Rafraîchir l'UI
                    }
                });
            }
        });
    }

    public void RecupererClassement()
    {
        if (db == null) return;

        string collectionName = estEnModeSpeedrun ? "ClassementSpeedrun" : "ClassementClassique";
        string champTri = estEnModeSpeedrun ? "temps" : "score";
        
        Query query = db.Collection(collectionName);

        if (estEnModeSpeedrun) query = query.OrderBy(champTri).Limit(50);
        else query = query.OrderByDescending(champTri).Limit(50);

        query.GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted || task.IsCanceled) 
            {
                Debug.LogError("🚨 ERREUR CHARGEMENT CLASSEMENT : " + task.Exception);
                return;
            }

            foreach (Transform enfant in conteneurClassement) 
            {
                Destroy(enfant.gameObject);
            }

            int rangActuel = 1;
            int monRang = -1;
            string monScoreTexte = "";

            foreach (DocumentSnapshot document in task.Result.Documents)
            {
                Dictionary<string, object> data = document.ToDictionary();
                string nomAffiche = data.ContainsKey("nom") ? data["nom"].ToString() : "Joueur";
                bool cEstMoi = (document.Id == userId);
                
                string texteScore = "";
                
                // ✅ CORRECTION 3 : Sécurisation ici aussi pour éviter que l'affichage ne plante
                if (estEnModeSpeedrun && data.ContainsKey("temps")) texteScore = FormaterScoreEnChrono(Convert.ToInt64(data["temps"]));
                else if (!estEnModeSpeedrun && data.ContainsKey("score")) texteScore = data["score"].ToString() + " pts";

                if (cEstMoi)
                {
                    monRang = rangActuel;
                    monScoreTexte = texteScore;
                }

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
        Parameter[] parametres = {
            new Parameter("niveau_atteint", niveau),
            new Parameter("score_final", score)
        };
        FirebaseAnalytics.LogEvent("joueur_est_mort", parametres);
    }

    public void AnalyserAchat(string nomObjet, int prix)
    {
        Parameter[] parametres = {
            new Parameter("nom_objet", nomObjet),
            new Parameter("prix_objet", prix)
        };
        FirebaseAnalytics.LogEvent("achat_boutique", parametres);
    }
}