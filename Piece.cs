using UnityEngine;
using System.Collections;

public class Piece : MonoBehaviour
{
    [Header("Réglages")]
    public int valeur = 1;

    [Header("Animation Infaillible")]
    public float hauteurDuHop = 1.0f;       
    public float dureeDuHop = 0.3f;         
    public float dureeDuVol = 0.4f;         
    public float echelleFinale = 0.2f;      
    public float distanceDevantCamera = 1.5f; 

    [Header("Effets Visuels")]
    public GameObject prefabScintillement;
    
    [Tooltip("Décalage vertical pour l'apparition des particules (ex: 0.5 pour monter un peu).")]
    public float decalageHauteurEffect = 0f; 
    [Tooltip("Avancer légèrement les particules vers la caméra pour qu'elles passent toujours 'devant' la pièce.")]
    public float avanceDevantPiece = 0.1f;

    [Header("Audio")]
    public AudioClip sonPiece;

    private bool estRamasse = false;
    private Collider monCollider;

    void Awake()
    {
        monCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (!estRamasse)
        {
            transform.Rotate(0, 100 * Time.deltaTime, 0);

            // --- NOUVEAU : LOGIQUE DE L'AIMANT ---
            if (PowerUpManager.instance != null && PowerUpManager.instance.aimantActif && GameManager.instance != null && GameManager.instance.joueurActuel != null)
            {
                float distance = Vector3.Distance(transform.position, GameManager.instance.joueurActuel.transform.position);
                
                // Si la pièce est à moins de 15 mètres du joueur, elle s'envole vers lui !
                if (distance < 15f) 
                {
                    // On vise un peu au-dessus du joueur pour que ça paraisse naturel (Vector3.up)
                    transform.position = Vector3.MoveTowards(transform.position, GameManager.instance.joueurActuel.transform.position + Vector3.up, 25f * Time.deltaTime);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (estRamasse) return;
            estRamasse = true; 

            if (monCollider != null) monCollider.enabled = false;

            if (sonPiece != null && AudioManager.instance != null)
            {
                AudioManager.instance.JouerSon(sonPiece);
            }

            if (prefabScintillement != null)
            {
                Vector3 positionSpawn = transform.position + new Vector3(0, decalageHauteurEffect, 0);

                if (Camera.main != null)
                {
                    Vector3 directionVersCamera = (Camera.main.transform.position - positionSpawn).normalized;
                    positionSpawn += directionVersCamera * avanceDevantPiece;
                }

                Instantiate(prefabScintillement, positionSpawn, Quaternion.identity);
            }

            StartCoroutine(RamassageInfaillible());
        }
    }

    private IEnumerator RamassageInfaillible()
    {
        // --- PHASE 1 : LE HOP ---
        Vector3 positionDepart = transform.position;
        float temps = 0f;
        
        while (temps < dureeDuHop)
        {
            temps += Time.deltaTime;
            float progression = temps / dureeDuHop;
            
            float yOffset = Mathf.Sin(progression * Mathf.PI) * hauteurDuHop;
            transform.position = positionDepart + new Vector3(0, yOffset, 0);
            
            transform.Rotate(0, 400 * Time.deltaTime, 0);
            yield return null; 
        }

        // --- PHASE 2 : VÉRIFICATION ---
        if (CoinMagnetManager.instance == null || CoinMagnetManager.instance.cibleArgentUI == null || Camera.main == null)
        {
            ValiderEtDetruire();
            yield break;
        }

        // --- PHASE 3 : LE VOL SANS LAG (CORRIGÉ) ---
        Camera mainCam = Camera.main; 
        
        // On mémorise la position de départ DANS LE MONDE (pas de SetParent !)
        Vector3 positionWorldDepart = transform.position; 
        Vector3 echelleDepart = transform.localScale;
        Vector3 echelleCible = echelleDepart * echelleFinale;

        temps = 0f;
        while (temps < dureeDuVol)
        {
            temps += Time.deltaTime;
            float progression = temps / dureeDuVol;
            
            float inverse = 1f - progression;
            float smooth = 1f - (inverse * inverse * inverse); 

            // On calcule où est l'icône UI dans le monde 3D
            Vector3 positionEcran = CoinMagnetManager.instance.cibleArgentUI.position;
            positionEcran.z = distanceDevantCamera;
            Vector3 cibleWorld = mainCam.ScreenToWorldPoint(positionEcran);
            
            // On déplace la pièce directement vers cette cible !
            transform.position = Vector3.Lerp(positionWorldDepart, cibleWorld, smooth);
            transform.localScale = Vector3.Lerp(echelleDepart, echelleCible, smooth);
            transform.Rotate(0, 400 * Time.deltaTime, 0);
            
            yield return null;
        }

        // --- PHASE 4 : ARRIVÉE ---
        ValiderEtDetruire();
    }

    private void ValiderEtDetruire()
    {
        if (GameManager.instance != null) GameManager.instance.AjouterArgent(valeur);
        if (QuestManager.instance != null) QuestManager.instance.AjouterProgression(TypeActionQuete.RamasserPieces, 1);
        
        // AU LIEU DE DÉTRUIRE :
        // 1. On remet la pièce "vierge" pour sa prochaine utilisation
        estRamasse = false; 
        if (monCollider != null) monCollider.enabled = true;
        
        // 2. On la cache et on la renvoie dans le réservoir
        gameObject.SetActive(false); 
        
        if (ObjectPooler.instance != null)
        {
            transform.SetParent(ObjectPooler.instance.transform);
        }
    }
}