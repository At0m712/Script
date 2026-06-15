using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using Firebase.Auth;
using Firebase.Extensions;

public class GooglePlayManager : MonoBehaviour
{
    public static GooglePlayManager instance;
    private FirebaseAuth auth;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // On active Google Play Games
        PlayGamesPlatform.Activate();
    }

    // Cette fonction sera appelée par le ProfileManager quand Firebase sera prêt
    public void LancerConnexionGoogleEtFirebase(FirebaseAuth firebaseAuth)
    {
        auth = firebaseAuth;
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("✅ [Google Play] Connecté ! Joueur : " + Social.localUser.userName);
            
            // On demande le code serveur pour le donner à Firebase
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, codeAuth => {
                ConnexionFirebaseAvecGoogle(codeAuth);
            });
        }
        else
        {
            Debug.LogWarning("⚠️ [Google Play] Échec de connexion. Statut : " + status + ". Connexion anonyme en secours...");
            // Si le joueur refuse ou n'a pas internet, on tente une connexion anonyme
            if (ProfileManager.instance != null) ProfileManager.instance.ConnecterAnonymement();
        }
    }

    private void ConnexionFirebaseAvecGoogle(string codeAuth)
    {
        Credential credential = PlayGamesAuthProvider.GetCredential(codeAuth);

        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 [Firebase] Erreur de lien avec Google : " + task.Exception);
                if (ProfileManager.instance != null) ProfileManager.instance.ConnecterAnonymement();
                return;
            }

            Debug.Log("✅ [Firebase] Joueur lié et authentifié avec succès ! UID : " + task.Result.User.UserId);
            
            // On lance la synchronisation de la base de données
            if (ProfileManager.instance != null) ProfileManager.instance.DemarrerSynchronisation(task.Result.User.UserId);
        });
    }
}