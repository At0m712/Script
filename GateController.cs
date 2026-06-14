using UnityEngine;

public class GateController : MonoBehaviour
{
    [Header("Éléments")]
    public Transform visuelMur;
    
    [Header("Animation")]
    public float hauteurOuverture = 5f;
    public float vitesseOuverture = 3f;

    public int ennemisRestants = 0;
    private Vector3 positionFermee;
    private Vector3 positionOuverte;
    private bool estOuverte = false;
    private bool joueurEstPasse = false;

    // Chronomètre de sécurité
    private float delaiAvantOuverture = 0f;

    void Start()
    {
        positionFermee = visuelMur.localPosition;
        positionOuverte = positionFermee + Vector3.up * hauteurOuverture;
    }

    public void AjouterEnnemi() { ennemisRestants++; }
    public void EnnemiTue() { ennemisRestants--; }

    void Update()
    {
        // On fait avancer le chronomètre
        delaiAvantOuverture += Time.deltaTime;

        // On empêche la porte de s'ouvrir pendant la 1ère seconde pour laisser le temps aux ennemis d'apparaître
        if (delaiAvantOuverture > 1f)
        {
            if (!joueurEstPasse && ennemisRestants <= 0 && ennemisRestants > -100)
            {
                estOuverte = true;
            }
        }

        // --- OPTIMISATION DE L'ANIMATION ---
        Vector3 cible = estOuverte ? positionOuverte : positionFermee;

        // On vérifie la distance entre la position actuelle et la destination
        if (Vector3.Distance(visuelMur.localPosition, cible) > 0.001f)
        {
            // La porte est encore loin, on fait le calcul complexe pour la déplacer de façon fluide
            visuelMur.localPosition = Vector3.Lerp(visuelMur.localPosition, cible, Time.deltaTime * vitesseOuverture);
        }
        else if (visuelMur.localPosition != cible)
        {
            // La porte est à moins d'un millimètre, on la "claque" sur sa cible exacte.
            // Au prochain tour (frame), le script ignorera totalement ces calculs !
            visuelMur.localPosition = cible;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && !joueurEstPasse && estOuverte)
        {
            // --- NOUVEAU : On vérifie que le joueur sort bien par l'AVANT (vers les Z positifs) ---
            if (other.transform.position.z > transform.position.z)
            {
                // Le joueur a complètement traversé l'épaisseur de la porte vers l'avant !
                joueurEstPasse = true;
                estOuverte = false; // Le mur se referme derrière lui

                // --- NOUVEAU : On fixe le point de respawn en sécurité ---
                // Au lieu de prendre la position du joueur, on force le point à 3 mètres devant la porte
                if (GameManager.instance != null)
                {
                    GameManager.instance.pointDeRespawn = transform.position + new Vector3(0f, 1f, 3f);
                }

                // On lance la génération de la suite
                if (LevelGenerator.instance != null)
                {
                    LevelGenerator.instance.JoueurPassePorte();
                }
            }
        }
    }
}