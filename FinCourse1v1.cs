using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Globalization;

public class FinCourse1v1 : MonoBehaviour
{
    private bool courseTerminee = false;
    private DatabaseReference dbRef;

    // 🚩 Les variables de sécurité pour le fil principal (Main Thread)
    private bool doitMettreAJourUI = false;
    private float monTempsEnregistre = 0f;
    private float tempsAdversaireRecu = 0f;

    void Start()
    {
        dbRef = FirebaseDatabase.GetInstance("https://leaderboardgame-5218c-default-rtdb.europe-west1.firebasedatabase.app/").GetReference("Salons_1v1");
        
        if (MatchmakingManager.instance.texteAttenteAdversaire != null) 
            MatchmakingManager.instance.texteAttenteAdversaire.SetActive(false);
            
        if (MatchmakingManager.instance.panelVictoire1v1 != null) 
            MatchmakingManager.instance.panelVictoire1v1.SetActive(false);
            
        if (MatchmakingManager.instance.panelDefaite1v1 != null) 
            MatchmakingManager.instance.panelDefaite1v1.SetActive(false);
    }

    void Update()
    {
        // ✅ C'est ici qu'on met à jour l'UI en toute sécurité, supervisé par Unity !
        if (doitMettreAJourUI)
        {
            doitMettreAJourUI = false;
            
            if (MatchmakingManager.instance.texteAttenteAdversaire != null) 
                MatchmakingManager.instance.texteAttenteAdversaire.SetActive(false);
                
            ComparerScores(monTempsEnregistre, tempsAdversaireRecu);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !courseTerminee)
        {
            courseTerminee = true;

            if (GameManager.instance != null && GameManager.instance.joueurRb != null)
            {
                GameManager.instance.joueurRb.linearVelocity = Vector3.zero;
                GameManager.instance.joueurRb.isKinematic = true; 
            }

            monTempsEnregistre = 0f;
            if (ChronoManager.instance != null) monTempsEnregistre = ChronoManager.instance.ObtenirTemps();
            
            Time.timeScale = 0f;
            Debug.Log($"Ligne franchie en {monTempsEnregistre}s ! Envoi au serveur...");
            
            GererFinDeCourse(monTempsEnregistre);
        }
    }

    void GererFinDeCourse(float monTemps)
    {
        DatabaseReference salonRef = dbRef.Child(MatchmakingManager.idDeMonSalon);
        salonRef.Child(MatchmakingManager.monRoleActuel).Child("chrono").SetValueAsync(monTemps);

        string roleAdversaire = (MatchmakingManager.monRoleActuel == "Joueur1") ? "Joueur2" : "Joueur1";

        salonRef.Child(roleAdversaire).Child("chrono").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted) return;

            if (task.IsCompleted && task.Result.Exists)
            {
                float tempsAdversaire = float.Parse(task.Result.Value.ToString(), CultureInfo.InvariantCulture);
                ComparerScores(monTemps, tempsAdversaire);
            }
            else
            {
                Debug.Log("L'adversaire n'a pas fini. En attente...");
                
                if (MatchmakingManager.instance.texteAttenteAdversaire != null) 
                    MatchmakingManager.instance.texteAttenteAdversaire.SetActive(true);
                    
                SurveillerArriveeAdversaire(salonRef.Child(roleAdversaire));
            }
        });
    }

    void SurveillerArriveeAdversaire(DatabaseReference refAdversaire)
    {
        refAdversaire.Child("chrono").ValueChanged += (sender, args) =>
        {
            if (args.Snapshot.Exists)
            {
                float tempsAdversaire = float.Parse(args.Snapshot.Value.ToString(), CultureInfo.InvariantCulture);
                if (tempsAdversaire > 0f)
                {
                    Debug.Log("L'adversaire vient de finir !");
                    // 🚩 On enregistre le score et on lève le drapeau pour l'Update !
                    tempsAdversaireRecu = tempsAdversaire;
                    doitMettreAJourUI = true;
                }
            }
        };
    }

    void ComparerScores(float monTemps, float tempsAdversaire)
    {
        string texteFinal = $"Ton temps : {monTemps:F2}s\nAdversaire : {tempsAdversaire:F2}s";

        if (monTemps < tempsAdversaire)
        {
            if (MatchmakingManager.instance.panelVictoire1v1 != null) MatchmakingManager.instance.panelVictoire1v1.SetActive(true);
        }
        else if (monTemps > tempsAdversaire)
        {
            if (MatchmakingManager.instance.panelDefaite1v1 != null) MatchmakingManager.instance.panelDefaite1v1.SetActive(true);
        }
        else
        {
            if (MatchmakingManager.instance.panelVictoire1v1 != null) MatchmakingManager.instance.panelVictoire1v1.SetActive(true);
            texteFinal = "ÉGALITÉ PARFAITE !\n" + texteFinal;
        }

        // ... (le début de la fonction avec les conditions de victoire/défaite reste pareil)

        // 👉 LA CORRECTION EST ICI :
        if (MatchmakingManager.instance.texteResultatDetaille != null) 
        {
            // 1. On allume le texte (vu qu'il était caché et indépendant)
            MatchmakingManager.instance.texteResultatDetaille.gameObject.SetActive(true);
            
            // 2. On écrit les temps dedans
            MatchmakingManager.instance.texteResultatDetaille.text = texteFinal;
        }
    }
}
