using UnityEngine;
using System.Collections;

public class AutoDesactivateur : MonoBehaviour
{
    [Tooltip("Temps en secondes avant que l'objet ne retourne dans la réserve")]
    public float dureeAvantDesactivation = 2f;

    void OnEnable()
    {
        // À chaque fois que l'objet sort de la réserve et s'allume, on lance le chrono
        StartCoroutine(DesactiverApresDelai());
    }

    private IEnumerator DesactiverApresDelai()
    {
        yield return new WaitForSeconds(dureeAvantDesactivation);
        
        // Au lieu de détruire (Destroy), on éteint l'objet. 
        // L'ObjectPooler le réutilisera automatiquement !
        gameObject.SetActive(false); 
    }
}