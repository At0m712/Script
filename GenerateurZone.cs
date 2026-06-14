using UnityEngine;

public class GenerateurZone : MonoBehaviour
{
    [Header("Ce qu'il faut faire apparaître")]
    [Tooltip("Glisse ici les objets de ta hiérarchie ou de tes prefabs")]
    public GameObject[] modelesAPlacer; 

    [Tooltip("C'est la quantité approximative que le générateur va essayer d'atteindre")]
    public int quantiteCible = 50;

    [Header("Taille de la Zone")]
    public Vector3 tailleZone = new Vector3(20f, 0f, 100f);

    [Header("Rangement (Optionnel)")]
    [Tooltip("Glisse un objet vide ici pour que tout se range dedans et ne pollue pas la hiérarchie")]
    public Transform dossierParent; 

    [ContextMenu("1. GÉNÉRER LES OBJETS (Répartition Optimisée)")]
    public void GenererObjetsGrille()
    {
        if (modelesAPlacer == null || modelesAPlacer.Length == 0)
        {
            Debug.LogWarning("Attention : Tu n'as mis aucun objet dans la liste 'Modeles A Placer' !");
            return;
        }

        // 1. Calcul de la grille pour une bonne répartition
        float ratioArea = tailleZone.x / tailleZone.z;
        int colonnes = Mathf.RoundToInt(Mathf.Sqrt(quantiteCible * ratioArea));
        colonnes = Mathf.Clamp(colonnes, 1, 1000); 
        int lignes = Mathf.RoundToInt((float)quantiteCible / colonnes);
        lignes = Mathf.Clamp(lignes, 1, 1000); 

        float spacingX = tailleZone.x / colonnes;
        float spacingZ = tailleZone.z / lignes;

        Debug.Log($"Génération optimisée de {colonnes * lignes} objets ({colonnes} cols x {lignes} lignes)");

        for (int x = 0; x < colonnes; x++)
        {
            for (int z = 0; z < lignes; z++)
            {
                // 2. Position de base au centre de la case
                Vector3 basePos = new Vector3(
                    (-tailleZone.x / 2f) + (spacingX / 2f) + (x * spacingX),
                    0f,
                    (-tailleZone.z / 2f) + (spacingZ / 2f) + (z * spacingZ)
                );

                // 3. Décalage aléatoire (Jitter)
                Vector3 offsetAléatoire = new Vector3(
                    Random.Range(-spacingX / 2f * 0.9f, spacingX / 2f * 0.9f),
                    0f,
                    Random.Range(-spacingZ / 2f * 0.9f, spacingZ / 2f * 0.9f)
                );

                // 4. Position finale
                Vector3 positionFinale = transform.position + basePos + offsetAléatoire;

                if (tailleZone.y > 0.01f)
                {
                    positionFinale.y += Random.Range(-tailleZone.y / 2f, tailleZone.y / 2f);
                }

                // 5. Choix de l'objet
                GameObject modeleChoisi = modelesAPlacer[Random.Range(0, modelesAPlacer.Length)];
                if (modeleChoisi == null) continue;

                // --- MODIFICATION : ROTATION Y ALÉATOIRE ---
                // On génère un angle entre 0 et 360 degrés
                float rotationY = Random.Range(0f, 360f); 

                Quaternion rotationFinale = Quaternion.Euler(
                    modeleChoisi.transform.eulerAngles.x, // Conserve X d'origine (pour ne pas piquer du nez)
                    rotationY,                            // Aléatoire sur Y (tourne sur lui-même)
                    modeleChoisi.transform.eulerAngles.z  // Conserve Z d'origine
                );

                // 6. Création
                GameObject nouvelObjet = Instantiate(modeleChoisi, positionFinale, rotationFinale);

                // 7. Rangement
                nouvelObjet.transform.parent = (dossierParent != null) ? dossierParent : this.transform;
            }
        }
    }

    [ContextMenu("2. TOUT EFFACER")]
    public void EffacerObjets()
    {
        Transform conteneur = dossierParent != null ? dossierParent : this.transform;
        for (int i = conteneur.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(conteneur.GetChild(i).gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f); Gizmos.DrawCube(transform.position, tailleZone);
        Gizmos.color = Color.green; Gizmos.DrawWireCube(transform.position, tailleZone);
    }
}