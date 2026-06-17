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
            // Le Power-up X2 s'applique uniquement ici, sur les points de l'ennemi tué !
            int pointsFinaux = pointsDonnes;
            if (PowerUpManager.instance != null && PowerUpManager.instance.x2Actif)
            {
                pointsFinaux *= 2;
            }
            
            GameManager.instance.AjouterScore(pointsFinaux);
        }

        Vector3 positionExplosion = transform.position + new Vector3(0f, 1.2f, 0f);

        if (ObjectPooler.instance != null && ObjectPooler.instance.dictionnaireReserves.ContainsKey(tagPoolExplosion))
        {
            ObjectPooler.instance.SortirObjet(tagPoolExplosion, positionExplosion, Quaternion.identity);
        }
        else if (explosionPrefab != null)
        {
            GameObject vfx = Instantiate(explosionPrefab, positionExplosion, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        if (CameraFollow.instance != null)
        {
            CameraFollow.instance.Secouer(1f, 0.25f);  
        }

        if (sonExplosion != null && AudioManager.instance != null)
        {
            AudioManager.instance.JouerSon(sonExplosion, volumeSon);
        }

        Destroy(gameObject);
    }
}