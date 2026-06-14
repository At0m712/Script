using UnityEngine;
using UnityEngine.SceneManagement; // Indispensable pour changer de scène !

public class FinTutoRetourJeu : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Écris ici le nom exact de ta scène principale (ex: SampleScene)")]
    public string nomScenePrincipale = "SampleScene"; 

    private bool aFranchiLaLigne = false;

    private void OnTriggerEnter(Collider other)
    {
        // Si c'est bien le joueur qui traverse la ligne d'arrivée
        if (other.CompareTag("Player") && !aFranchiLaLigne)
        {
            aFranchiLaLigne = true; // Sécurité pour éviter de charger 2 fois
            TerminerEtQuitter();
        }
    }

    private void TerminerEtQuitter()
    {
        Debug.Log("Tutoriel terminé ! Retour au menu principal.");

        // 1. On sauvegarde définitivement la réussite du tuto
        PlayerPrefs.SetInt("TutoInteractifFini", 1);
        PlayerPrefs.Save();

        // 2. On remet le temps à la normale (au cas où il était en pause)
        Time.timeScale = 1f;

        // 3. On téléporte le joueur dans la scène du vrai jeu
        SceneManager.LoadScene(nomScenePrincipale);
    }
}