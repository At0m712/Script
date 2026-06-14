using UnityEngine;

public class ApparitionMur : MonoBehaviour
{
    [Header("Paramètres du Mur")]
    public Transform murAFaireMonter; // Glisse ton objet mur ici
    public float hauteurAMonter = 3f; // De combien de mètres le mur doit monter
    public float vitesseMontee = 5f;  // La vitesse d'animation du mur

    private Vector3 positionCible;
    private bool declenche = false;

    void Start()
    {
        // On calcule la position finale du mur (sa position de départ + 3 mètres vers le haut)
        if (murAFaireMonter != null)
        {
            positionCible = murAFaireMonter.position + new Vector3(0, hauteurAMonter, 0);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si le joueur rentre dans la zone et que le mur n'est pas déjà déclenché
        if (other.CompareTag("Player") && !declenche)
        {
            declenche = true; // On active l'animation !
        }
    }

    void Update()
    {
        // Si la zone a été touchée, on déplace le mur petit à petit vers le haut
        if (declenche && murAFaireMonter != null)
        {
            murAFaireMonter.position = Vector3.MoveTowards(
                murAFaireMonter.position, 
                positionCible, 
                vitesseMontee * Time.deltaTime
            );
        }
    }
}