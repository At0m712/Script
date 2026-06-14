using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LigneLeaderboard : MonoBehaviour
{
    public TMP_Text texteInfos;
    public Image imageFond;

    [Header("Couleurs de Fond")]
    public Color couleurTop1 = new Color(1f, 0.84f, 0f, 1f); // Or
    public Color couleurTop2 = new Color(0.75f, 0.75f, 0.75f, 1f); // Argent
    public Color couleurTop3 = new Color(0.8f, 0.5f, 0.2f, 1f); // Bronze
    public Color couleurNormale = new Color(0f, 0f, 0f, 0.5f); // Sombre

    // MODIFICATION ICI : 'int score' devient 'string scoreTexte'
    public void ConfigurerLigne(int rang, string nom, string scoreTexte, bool cEstMoi, bool estLigneDuBas = false)
    {
        // On affiche directement le texte tel quel (le Manager décidera si c'est des points ou un chrono)
        texteInfos.text = rang + ". " + nom + " - " + scoreTexte;

        // --- CAS 1 : LA LIGNE FIXE EN BAS (Priorité Absolue) ---
        if (estLigneDuBas)
        {
            imageFond.color = Color.yellow; // Fond JAUNE
            texteInfos.color = Color.black; // Texte NOIR
            return; // On stoppe ici, on ne regarde pas le reste
        }

        // --- CAS 2 : LES LIGNES DU CLASSEMENT (TOP 50) ---
        // On définit le fond selon le rang uniquement
        if (rang == 1) imageFond.color = couleurTop1;
        else if (rang == 2) imageFond.color = couleurTop2;
        else if (rang == 3) imageFond.color = couleurTop3;
        else imageFond.color = couleurNormale;

        // On définit la couleur du texte
        if (cEstMoi)
        {
            // Si tu es dans le Top 3, texte noir pour lire sur l'or/argent/bronze
            // Sinon, ton texte devient JAUNE sur le fond sombre
            texteInfos.color = (rang <= 3) ? Color.black : Color.yellow;
        }
        else
        {
            // Pour les autres joueurs
            texteInfos.color = (rang <= 3) ? Color.black : Color.white;
        }
    }
}