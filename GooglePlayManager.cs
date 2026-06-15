using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

public class GooglePlayManager : MonoBehaviour
{
    void Start()
    {
        // 1. On active le plugin Google Play Games au lancement
        PlayGamesPlatform.Activate();
        
        // 2. On lance la tentative de connexion
        ConnexionGooglePlay();
    }

    public void ConnexionGooglePlay()
    {
        // Demande à Google de connecter l'utilisateur
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
    }

    // Cette fonction complète gère le résultat de la connexion
    internal void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            // Si ça marche, on affiche un message dans la console
            Debug.Log("Connexion Google Play réussie ! Bienvenue : " + Social.localUser.userName);
        }
        else
        {
            // Si ça échoue, on affiche l'erreur
            Debug.Log("Échec de la connexion. Statut : " + status);
        }
    }
}