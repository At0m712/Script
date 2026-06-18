using UnityEngine;
using System.Collections;

public class AnimationRebondUI : MonoBehaviour
{
    [Header("Paramètres du Rebond")]
    [Tooltip("La taille maximum que l'objet va atteindre")]
    public Vector3 echelleMax = new Vector3(1.2f, 1.2f, 1.2f); 
    [Tooltip("Le temps pour faire UN seul rebond (Aller-Retour)")]
    public float dureeAllerRetour = 0.3f; 
    
    [Header("Rythme (Ex: TUN TUN ... Pause)")]
    [Tooltip("Combien de rebonds rapides à la suite ? (2 = TUN TUN)")]
    public int rebondsALaSuite = 2; 
    [Tooltip("La micro-pause entre les rebonds rapides (en secondes)")]
    public float pauseEntreRebondsRapides = 0.05f; 
    [Tooltip("La GRANDE pause après la série (en secondes)")]
    public float pauseLongueApresSerie = 1.5f; 

    [Header("Répétition globale")]
    [Tooltip("Si coché, l'animation ne s'arrêtera jamais")]
    public bool infini = true; 
    [Tooltip("Si infini n'est pas coché, combien de séries fait-on au total ?")]
    public int nombreDeSeries = 3; 

    private Vector3 echelleInitiale = Vector3.zero;
    private Coroutine routineAnimation;

    void OnEnable()
    {
        // Enregistrer la taille de base au premier lancement
        if (echelleInitiale == Vector3.zero)
        {
            echelleInitiale = transform.localScale;
        }
        
        // S'assurer qu'on repart de la bonne taille
        transform.localScale = echelleInitiale;
        
        // Démarrer l'animation
        if (routineAnimation != null) StopCoroutine(routineAnimation);
        routineAnimation = StartCoroutine(FaireSéquences());
    }

    void OnDisable()
    {
        // Remettre l'objet à sa taille normale si on ferme le menu
        if (echelleInitiale != Vector3.zero)
        {
            transform.localScale = echelleInitiale;
        }
    }

    IEnumerator FaireSéquences()
    {
        int seriesFaites = 0;

        while (infini || seriesFaites < nombreDeSeries)
        {
            // 1. Jouer la série de rebonds rapides (Le "TUN TUN")
            for (int i = 0; i < rebondsALaSuite; i++)
            {
                // Joue le rebond
                yield return StartCoroutine(AnimerUnRebond());

                // Fait une petite pause si on n'est pas au dernier rebond de la série
                if (i < rebondsALaSuite - 1 && pauseEntreRebondsRapides > 0f)
                {
                    yield return new WaitForSecondsRealtime(pauseEntreRebondsRapides);
                }
            }

            seriesFaites++;

            // 2. Faire la grande pause (Le "Pause")
            if (pauseLongueApresSerie > 0f && (infini || seriesFaites < nombreDeSeries))
            {
                yield return new WaitForSecondsRealtime(pauseLongueApresSerie);
            }
        }
    }

    IEnumerator AnimerUnRebond()
    {
        float temps = 0f;
        float moitieDuree = dureeAllerRetour / 2f;

        // Phase 1 : Grossir
        while (temps < moitieDuree)
        {
            temps += Time.unscaledDeltaTime; 
            float progression = temps / moitieDuree;
            
            progression = Mathf.SmoothStep(0f, 1f, progression); 
            
            transform.localScale = Vector3.Lerp(echelleInitiale, echelleMax, progression);
            yield return null;
        }

        temps = 0f;

        // Phase 2 : Rapetisser
        while (temps < moitieDuree)
        {
            temps += Time.unscaledDeltaTime;
            float progression = temps / moitieDuree;
            
            progression = Mathf.SmoothStep(0f, 1f, progression);
            
            transform.localScale = Vector3.Lerp(echelleMax, echelleInitiale, progression);
            yield return null;
        }

        // Sécurité
        transform.localScale = echelleInitiale;
    }
}