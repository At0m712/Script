using System.Collections;
using UnityEngine;

public class LaserClignotant : MonoBehaviour
{
    [Header("L'objet à activer/désactiver")]
    [Tooltip("Si tu laisses vide, le script utilisera l'objet sur lequel il est placé.")]
    public GameObject objetLaser;

    [Header("Réglages du chronomètre")]
    public float tempsAllume = 2f; // Le laser reste allumé 2 secondes
    public float tempsEteint = 3f; // Le laser reste éteint 3 secondes

    [Header("État de départ")]
    public bool allumeAuDemarrage = true; // Est-ce qu'il commence allumé ou éteint ?

    void Start()
    {
        // Sécurité : Si on a oublié de glisser un objet, on prend l'objet actuel
        if (objetLaser == null)
        {
            objetLaser = this.gameObject; 
        }

        // On lance la machine infernale !
        StartCoroutine(RoutineClignotement());
    }

    private IEnumerator RoutineClignotement()
    {
        // On initialise l'état actuel
        bool estAllume = allumeAuDemarrage;

        // Cette boucle tourne à l'infini (tant que l'objet n'est pas détruit)
        while (true)
        {
            // 1. On applique l'état (Allumé ou Éteint)
            objetLaser.SetActive(estAllume);

            // 2. On fait une pause selon l'état
            if (estAllume)
            {
                // Si le laser est allumé, on attend le temps d'allumage
                yield return new WaitForSeconds(tempsAllume);
            }
            else
            {
                // Si le laser est éteint, on attend le temps de pause
                yield return new WaitForSeconds(tempsEteint);
            }

            // 3. On inverse l'état pour le prochain tour !
            // (Si c'était true ça devient false, si c'était false ça devient true)
            estAllume = !estAllume;
        }
    }
}