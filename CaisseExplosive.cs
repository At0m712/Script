using UnityEngine;

public class CaisseExplosive : MonoBehaviour
{
    [Header("Réglages de l'explosion")]
    public float rayonExplosion = 5f;       
    public float forceExplosion = 700f;     
    public float forceSoulevement = 1f;     

    [Header("Visuel")]
    public GameObject effetParticules;  
    
    [Tooltip("Le nom de la réserve dans ton ObjectPooler (ex: 'ExplosionCaisse')")]
    public string tagPoolExplosion = "ExplosionCaisse";  
    
    [Header("Audio")]
    public AudioClip sonExplosion;
    [Range(0f, 1f)] public float volumeSon = 1f;  

    private bool aExplose = false;

    // Tableau pré-alloué pour éviter le Garbage Collector
    private Collider[] bufferObjetsTouches = new Collider[30];

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !aExplose)
        {
            Exploser();
        }
    }

    public void Exploser()
    {
        aExplose = true;

        // --- OPTIMISATION : Utilisation de l'Object Pooler ---
        if (ObjectPooler.instance != null && ObjectPooler.instance.dictionnaireReserves.ContainsKey(tagPoolExplosion))
        {
            ObjectPooler.instance.SortirObjet(tagPoolExplosion, transform.position, Quaternion.identity);
        }
        else if (effetParticules != null)
        {
            GameObject vfx = Instantiate(effetParticules, transform.position, Quaternion.identity);
            Destroy(vfx, 2f); 
        }

        // L'APPEL OPTIMISÉ (NonAlloc)
        int nombreDObjetsTouches = Physics.OverlapSphereNonAlloc(transform.position, rayonExplosion, bufferObjetsTouches);

        for (int i = 0; i < nombreDObjetsTouches; i++)
        {
            Collider objet = bufferObjetsTouches[i];

            // 🎯 LE TRI PAR TAG : On n'applique la force QUE si l'objet est le joueur
            if (objet.CompareTag("Player"))
            {
                if (objet.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddExplosionForce(forceExplosion, transform.position, rayonExplosion, forceSoulevement, ForceMode.Impulse);
                }
            }
        }
        
        // --- Jouer le son de l'explosion ---
        if (sonExplosion != null && AudioManager.instance != null)
        {
            AudioManager.instance.JouerSon(sonExplosion, volumeSon);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rayonExplosion);
    }
}