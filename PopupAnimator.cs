using UnityEngine;
using System.Collections;

public class PopupAnimator : MonoBehaviour
{
    [Header("Paramètres d'Animation")]
    public float vitesseAnimation = 0.25f;
    
    [Tooltip("La taille finale du panneau quand il est ouvert (Généralement X:1, Y:1, Z:1)")]
    public Vector3 tailleOuverte = Vector3.one; // ✅ NOUVEAU : Tu peux choisir la taille max !

    public void Ouvrir()
    {
        // Si elle est déjà ouverte et à sa taille max, on ne fait rien
        if (gameObject.activeSelf && transform.localScale == tailleOuverte) return;

        // Si la popup était désactivée, on force sa taille à 0 avant de l'allumer
        if (!gameObject.activeSelf)
        {
            transform.localScale = Vector3.zero;
        }

        gameObject.SetActive(true);
        StopAllCoroutines();
        // On va jusqu'à 'tailleOuverte' au lieu de Vector3.one
        StartCoroutine(Animer(transform.localScale, tailleOuverte, false)); 
    }

    public void Fermer()
    {
        if (!gameObject.activeInHierarchy) return;

        StopAllCoroutines();
        StartCoroutine(Animer(transform.localScale, Vector3.zero, true));
    }

    private IEnumerator Animer(Vector3 startScale, Vector3 endScale, bool cacherALaFin)
    {
        float tempsEcoule = 0f;
        transform.localScale = startScale;

        while (tempsEcoule < vitesseAnimation)
        {
            tempsEcoule += Time.unscaledDeltaTime; 
            float progression = tempsEcoule / vitesseAnimation;
            float smoothProgression = Mathf.SmoothStep(0f, 1f, progression); 
            transform.localScale = Vector3.Lerp(startScale, endScale, smoothProgression);
            yield return null;
        }

        transform.localScale = endScale;
        if (cacherALaFin) gameObject.SetActive(false);
    }
}