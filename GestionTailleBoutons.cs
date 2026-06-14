using UnityEngine;

public class GestionTailleBoutons : MonoBehaviour
{
    [Header("Les boutons (Glisser les RectTransform)")]
    public RectTransform boutonClassique;
    public RectTransform boutonSpeedrun;

    [Header("Taille du bouton sélectionné")]
    public float tailleActive = 1.2f;

    // Fonction à appeler quand on clique sur Classique
    public void GrossirClassique()
    {
        boutonClassique.localScale = new Vector3(tailleActive, tailleActive, 1f);
        boutonSpeedrun.localScale = Vector3.one; // Remet l'autre à la taille 1
    }

    // Fonction à appeler quand on clique sur Speedrun
    public void GrossirSpeedrun()
    {
        boutonSpeedrun.localScale = new Vector3(tailleActive, tailleActive, 1f);
        boutonClassique.localScale = Vector3.one; // Remet l'autre à la taille 1
    }
}