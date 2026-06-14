using UnityEngine;
using UnityEngine.SceneManagement;

public class ControleurLancement : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Le nom de ta scène de jeu normale")]
    public string nomScenePrincipale = "SampleScene"; 

    void Awake()
    {
        // On vérifie immédiatement dans la mémoire du téléphone
        // Si la valeur est à 1, ça veut dire que le tuto a déjà été fini dans le passé
        if (PlayerPrefs.GetInt("TutoInteractifFini", 0) == 1)
        {
            Debug.Log("Ancien joueur détecté : On zappe le tuto !");
            
            // On charge instantanément la scène principale (Le Menu / Jeu normal)
            SceneManager.LoadScene(nomScenePrincipale);
        }
        else
        {
            // C'est un nouveau joueur (la valeur est à 0 par défaut).
            // On ne fait rien, on le laisse simplement dans la scène du tuto !
            Debug.Log("Nouveau joueur : Début du tutoriel.");
        }
    }
}