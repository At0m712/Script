using UnityEngine;

public class DeathZone : MonoBehaviour
{
    // On crée une variable pour garder la caméra en mémoire
    private Transform camTransform;

    void Start()
    {
        // On cherche la caméra UNE SEULE FOIS au démarrage
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // ASTUCE JEU INFINI : La zone de mort glisse sous le joueur en suivant la caméra mémorisée
        if (camTransform != null)
        {
            Vector3 positionCamera = camTransform.position;
            transform.position = new Vector3(positionCamera.x, transform.position.y, positionCamera.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.instance != null && GameManager.instance.PerdreVie() == true)
            {
                RespawnJoueur(other.gameObject);
            }
        }
    }

    void RespawnJoueur(GameObject joueur)
    {
        // 1. On coupe la physique pour éviter les bugs de vitesse
        Rigidbody rb = joueur.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 2. On téléporte le joueur au point de Respawn dynamique du GameManager !
        if (GameManager.instance != null)
        {
            joueur.transform.position = GameManager.instance.pointDeRespawn;
        }
        else 
        {
            // Sécurité au cas où
            joueur.transform.position = new Vector3(0, 2f, 0); 
        }

        joueur.transform.rotation = Quaternion.identity;
    }
}