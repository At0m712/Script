using UnityEngine;
using UnityEngine.SceneManagement;

public class AiguilleurTuto : MonoBehaviour
{
    void Start()
    {
        // Dès que l'application s'ouvre, on vérifie la mémoire
        if (PlayerPrefs.GetInt("TutoFini", 0) == 1)
        {
            // C'est un ancien joueur ! 
            // On zappe le tuto et on charge ta scène principale avec le menu.
            // (Le joueur ne verra même pas que cette scène s'est ouverte)
            SceneManager.LoadScene("SampleScene"); 
        }
        else
        {
            // C'est un nouveau ! On ne fait rien, on le laisse jouer au tuto.
            Debug.Log("Nouveau joueur : début du tutoriel.");
        }
    }
}