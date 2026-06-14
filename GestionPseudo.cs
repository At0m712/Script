using UnityEngine;
using TMPro;

public class GestionPseudo : MonoBehaviour
{
    [Header("Interface")]
    public GameObject panelChoixPseudo;
    public TMP_InputField champSaisiePseudo;

    void Start()
    {
        string pseudoActuel = PlayerPrefs.GetString("MonPseudoFirebase", "joueur");

        if (string.IsNullOrEmpty(pseudoActuel) || pseudoActuel == "joueur")
        {
            panelChoixPseudo.SetActive(true); 
        }
        else
        {
            panelChoixPseudo.SetActive(false); 
        }
    }

    public void ValiderNouveauPseudo()
    {
        // 🛡️ SÉCURITÉ ANDROID/FIREBASE : On retire les caractères interdits pour éviter de corrompre le JSON
        string pseudoSaisi = champSaisiePseudo.text.Trim()
            .Replace(".", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("[", "")
            .Replace("]", "");

        if (!string.IsNullOrEmpty(pseudoSaisi) && pseudoSaisi.ToLower() != "joueur")
        {
            int tagAleatoire = UnityEngine.Random.Range(1000, 10000);
            string pseudoFinal = pseudoSaisi + "#" + tagAleatoire;

            if (FirebaseManager.instance != null)
            {
                FirebaseManager.instance.DefinirPseudo(pseudoFinal);
            } 

            panelChoixPseudo.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Saisie refusée : Le pseudo est vide ou contient uniquement des caractères interdits.");
        }
    }
}