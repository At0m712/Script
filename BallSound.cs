using UnityEngine;

public class BallSound : MonoBehaviour
{
    [Header("Fichier Audio")]
    public AudioClip sonRebond;

    [Header("Réglages")]
    public float vitesseMinimale = 1.0f; 
    public float puissanceMax = 15f;    

    void OnCollisionEnter(Collision collision)
    {
        
        Vector3 normalSurface = collision.GetContact(0).normal;

        
        float chocFrontal = Vector3.Dot(collision.relativeVelocity, normalSurface);
        chocFrontal = Mathf.Abs(chocFrontal);

        
        if (chocFrontal > vitesseMinimale)
        {
            
            float volumeImpact = Mathf.Clamp01(chocFrontal / puissanceMax);
            
            if (sonRebond != null && AudioManager.instance != null)
            {
                
                AudioManager.instance.JouerSon(sonRebond, volumeImpact);
            }
        }
    }
}