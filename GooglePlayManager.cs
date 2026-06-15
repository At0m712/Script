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
        // Déplacé dans Awake pour garantir l'activation avant toute connexion
        PlayGamesPlatform.Activate();
    }

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
            
            PlayGamesPlatform.Instance.RequestServerSideAccess(true, codeAuth => {
                // SÉCURITÉ : Vérifie si le Setup Unity a bien le Client Web ID
                if(string.IsNullOrEmpty(codeAuth))
                {
                    Debug.LogError("🚨 [Google Play] Le Code d'accès serveur est VIDE. Firebase ne peut pas se lier ! Vérifiez que vous avez bien mis l'ID Client Web (et non Android) dans le menu Window > Google Play Games > Setup.");
                    if (ProfileManager.instance != null) ProfileManager.instance.ConnecterAnonymement();
                    return;
                }
                
                ConnexionFirebaseAvecGoogle(codeAuth);
            });
        }
        else
        {
            Debug.LogWarning("⚠️ [Google Play] Échec de connexion (Normal si vous testez sur PC Unity). Statut : " + status + ". -> Lancement de la connexion Anonyme...");
            if (ProfileManager.instance != null) ProfileManager.instance.ConnecterAnonymement();
        }
    }

    private void ConnexionFirebaseAvecGoogle(string codeAuth)
    {
        Credential credential = PlayGamesAuthProvider.GetCredential(codeAuth);

        auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task => {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("🚨 [Firebase] Erreur de lien Google/Firebase : " + task.Exception);
                if (ProfileManager.instance != null) ProfileManager.instance.ConnecterAnonymement();
                return;
            }

            Debug.Log("✅ [Firebase] Joueur Play Games lié et authentifié avec succès ! UID : " + task.Result.User.UserId);
            
            if (ProfileManager.instance != null) ProfileManager.instance.DemarrerSynchronisation(task.Result.User.UserId);
        });
    }
}