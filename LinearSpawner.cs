using UnityEngine;

public class LinearSpawner : MonoBehaviour
{
    public GameObject prefabASelectionner; // Ton gradin
    public float longueurPiste = 1000f;
    public float largeurObjet = 36f;

    [ContextMenu("Générer les objets")] // Permet de lancer le script depuis l'Inspector
    public void Generer() 
    {
        // 1. Calcul du nombre d'objets (Division entière)
        int nombreObjets = Mathf.FloorToInt(longueurPiste / largeurObjet);

        for (int i = 0; i < nombreObjets; i++)
        {
            // 2. Calcul de la position X ou Z (selon l'orientation de ta piste)
            // On multiplie l'index (0, 1, 2...) par la largeur de l'objet
            float positionZ = i * largeurObjet;

            Vector3 positionFinale = transform.position + new Vector3(0, 0, positionZ);

            // 3. Création de l'objet
            GameObject nouvelObjet = Instantiate(prefabASelectionner, positionFinale, prefabASelectionner.transform.rotation);
            
            // Optionnel : On le met en enfant pour ne pas polluer la Hierarchy
            nouvelObjet.transform.parent = this.transform;
        }

        Debug.Log($"Placement terminé : {nombreObjets} objets créés.");
    }
}