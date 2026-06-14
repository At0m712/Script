using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Réglages")]
    public int pointsDonnes = 50;

    [Header("Effets Visuels (Optionnel)")]
    public GameObject explosionPrefab; 
    
    [Tooltip("Le nom de la réserve dans ton ObjectPooler (ex: 'Explosion')")]
    public string tagPoolExplosion = "Explosion";

    [Header("Effets Sonores (Optionnel)")]
    public AudioClip sonExplosion;      
    [Range(0f, 1f)] public float volumeSon = 1f;

    [HideInInspector] public GateController maPorte; 

    private bool estMort = false;

    void OnDestroy()
    {
        if (maPorte != null)
        {
            maPorte.EnnemiTue();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Exploser();
        }
    }

    void Exploser()
    {
        if (estMort) return;
        estMort = true;

        if (QuestManager.instance != null) 
        {
            QuestManager.instance.AjouterProgression(TypeActionQuete.TuerEnnemis, 1);
        }

        if (GameManager.instance != null) 
        {
            GameManager.instance.AjouterScore(pointsDonnes);
        }

        Vector3 positionExplosion = transform.position + new Vector3(0f, 1.2f, 0f);

        // --- OPTIMISATION : Utilisation de l'Object Pooler ---
        if (ObjectPooler.instance != null && ObjectPooler.instance.dictionnaireReserves.ContainsKey(tagPoolExplosion))
        {
            // On sort une explosion de la réserve (Zéro lag !)
            ObjectPooler.instance.SortirObjet(tagPoolExplosion, positionExplosion, Quaternion.identity);
        }
        else if (explosionPrefab != null)
        {
            // Sécurité : Si tu as oublié de paramétrer l'ObjectPooler, on utilise l'ancienne méthode
            GameObject vfx = Instantiate(explosionPrefab, positionExplosion, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        // Appel instantané de la caméra
        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Secouer(1f, 0.25f);  
        }

        if (sonExplosion != null && AudioManager.instance != null)
        {
            AudioManager.instance.JouerSon(sonExplosion, volumeSon);
        }

        // (On détruit l'ennemi pour l'instant car ton SpawnManager utilise des Instantiate pour les créer. 
        // L'idéal plus tard sera de modifier le SpawnManager pour utiliser le Pooler aussi !)
        Destroy(gameObject);
    }
}