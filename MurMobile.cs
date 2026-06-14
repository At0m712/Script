using UnityEngine;

// Cette ligne force Unity à ajouter un Rigidbody automatiquement si tu l'oublies !
[RequireComponent(typeof(Rigidbody))] 
public class MurMobile : MonoBehaviour
{
    [Header("Réglages du mouvement")]
    [Tooltip("La vitesse à laquelle le mur fait des allers-retours.")]
    public float vitesse = 3f;
    
    [Tooltip("La distance maximum que le mur peut parcourir depuis son centre.")]
    public float distance = 5f;

    private Vector3 positionDepart;
    private Rigidbody rb;

    void Start()
    {
        // On mémorise la position exacte de l'objet au lancement du jeu
        positionDepart = transform.position;
        
        // On récupère le composant physique
        rb = GetComponent<Rigidbody>();
        
        // SÉCURITÉ : On force le mur à être "Kinematic" pour qu'il ne tombe pas avec la gravité
        rb.isKinematic = true; 
    }

    // On utilise FixedUpdate au lieu de Update quand on manipule la physique !
    void FixedUpdate() 
    {
        // Le calcul mathématique reste le même
        float balancier = Mathf.Sin(Time.time * vitesse) * distance;
        Vector3 nouvellePosition = positionDepart + (transform.right * balancier);

        // LA MAGIE EST ICI : On demande au moteur physique de déplacer le mur.
        // Ainsi, il va physiquement pousser tout ce qui se trouve sur son chemin !
        rb.MovePosition(nouvellePosition);
    }
}