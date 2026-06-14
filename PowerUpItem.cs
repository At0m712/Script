using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    // On crée une liste déroulante pour choisir le type de bonus dans l'inspecteur
    public enum TypePowerUp { Aimant, X2, VieSup }
    public TypePowerUp typeDeCeBonus;

    [Header("Effets Visuels & Sonores")]
    public AudioClip sonRamassage;
    public GameObject particulesRamassage;
    [Header("Animation")]
    public Vector3 vitesseRotation = new Vector3(0, 100, 0); // Vitesse par défaut

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. On joue le son
            if (sonRamassage != null && AudioManager.instance != null)
            {
                AudioManager.instance.JouerSon(sonRamassage);
            }

            // 2. On lance les particules
            if (particulesRamassage != null)
            {
                Instantiate(particulesRamassage, transform.position, Quaternion.identity);
            }

            // 3. On applique le bon effet !
            switch (typeDeCeBonus)
            {
                case TypePowerUp.Aimant:
                    if (PowerUpManager.instance != null) PowerUpManager.instance.ActiverAimant();
                    break;
                case TypePowerUp.X2:
                    if (PowerUpManager.instance != null) PowerUpManager.instance.ActiverX2();
                    break;
                case TypePowerUp.VieSup:
                    if (GameManager.instance != null) GameManager.instance.AjouterVie();
                    break;
            }

            // 4. On détruit la pièce spéciale
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Utilise la vitesse personnalisée au lieu d'un chiffre bloqué
        transform.Rotate(vitesseRotation * Time.deltaTime, Space.World); 
    }
}