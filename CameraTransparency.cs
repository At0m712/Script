using UnityEngine;
using System.Collections.Generic;

public class CameraTransparency : MonoBehaviour
{
    [Header("Cibles")]
    public Transform target;
    public LayerMask obstaclesMask;

    [Header("Paramètres de Transparence")]
    [Range(0f, 1f)]
    public float opaciteCible = 0.3f; 

    private Dictionary<MeshRenderer, Color> objetsTransparents = new Dictionary<MeshRenderer, Color>();

    // OPTIMISATION 1 : Tableau pré-alloué pour éviter de créer de la mémoire à chaque frame
    private RaycastHit[] hits = new RaycastHit[20]; 

    // OPTIMISATION 2 : Convertir les noms des propriétés du Shader en identifiants (ID) ultra-rapides
    private static readonly int SurfaceID = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendID = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendID = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteID = Shader.PropertyToID("_ZWrite");

    void Update()
    {
        if (!target) return;

        Vector3 direction = target.position - transform.position;
        float distance = direction.magnitude;

        // Utilisation de RaycastNonAlloc : Ne crée AUCUN déchet en mémoire !
        int nombreDeHits = Physics.RaycastNonAlloc(transform.position, direction, hits, distance - 0.5f, obstaclesMask);
        
        List<MeshRenderer> objetsTouchesCeTour = new List<MeshRenderer>();

        // 1. RENDRE TRANSPARENT
        // On ne boucle que sur le nombre d'objets réellement touchés par le rayon
        for (int i = 0; i < nombreDeHits; i++)
        {
            // --- NOUVELLE OPTIMISATION : TryGetComponent ---
            // Beaucoup plus rapide et propre que GetComponent !
            if (hits[i].collider.TryGetComponent<MeshRenderer>(out MeshRenderer renderer))
            {
                objetsTouchesCeTour.Add(renderer);

                if (!objetsTransparents.ContainsKey(renderer))
                {
                    // Sauvegarde
                    Color couleurOrigine = renderer.material.color;
                    objetsTransparents.Add(renderer, couleurOrigine);

                    // Forcer le mode Transparent pour URP
                    ForcerURPTransparent(renderer.material);

                    // Appliquer l'opacité
                    Color couleurTransparente = couleurOrigine;
                    couleurTransparente.a = opaciteCible;
                    renderer.material.color = couleurTransparente;
                }
            }
        }

        // 2. RESTAURER L'OPACITÉ
        List<MeshRenderer> clesASupprimer = new List<MeshRenderer>();

        foreach (var kvp in objetsTransparents)
        {
            MeshRenderer renderer = kvp.Key;

            if (!objetsTouchesCeTour.Contains(renderer))
            {
                if (renderer != null)
                {
                    // On remet la couleur
                    renderer.material.color = kvp.Value;
                    
                    // On force le retour au mode Opaque
                    ForcerURPOpaque(renderer.material);
                }
                clesASupprimer.Add(renderer);
            }
        }

        // 3. NETTOYAGE
        foreach (MeshRenderer renderer in clesASupprimer)
        {
            objetsTransparents.Remove(renderer);
        }
    }

    // --- FONCTIONS OPTIMISÉES POUR URP LIT ---

    void ForcerURPTransparent(Material mat)
    {
        mat.SetFloat(SurfaceID, 1f); 
        mat.SetInt(SrcBlendID, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt(DstBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt(ZWriteID, 0); 
        mat.renderQueue = 3000;   
    }

    void ForcerURPOpaque(Material mat)
    {
        mat.SetFloat(SurfaceID, 0f); 
        mat.SetInt(SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt(DstBlendID, (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt(ZWriteID, 1); 
        mat.renderQueue = 2000;   
    }
}