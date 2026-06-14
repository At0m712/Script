using UnityEngine;
using UnityEngine.SceneManagement;

public class FinDeTuto : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. On sauvegarde que le joueur a réussi le tuto !
            PlayerPrefs.SetInt("TutoFini", 1);
            PlayerPrefs.Save();

            // 2. On l'envoie dans ta scène principale
            // Il atterrira face à ton Menu, prêt à cliquer sur "Jouer" !
            SceneManager.LoadScene("SampleScene"); 
        }
    }
}