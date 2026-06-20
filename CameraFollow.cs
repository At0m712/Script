using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    public Transform target;       
    
    [Header("Réglages Position")]
    public Vector3 offset; 
    
    [Header("Rotation UI (Amortisseur)")]
    public float angleRotation = 0f; 
    public float fluiditeRotation = 15f; 
    private float angleRotationLisse = 0f; 
    
    [Header("Animation Position (Plus c'est bas, plus c'est réactif)")]
    // Avec SmoothDamp, on ne parle plus de "Vitesse" mais de "Temps de réaction"
    public float tempsTransition = 0.5f; 
    public float tempsSuivi = 0.1f;      

    private bool estEnTransition = false;
    private Quaternion rotationChoisie; 

    private Vector3 offsetSecousse = Vector3.zero;
    private Coroutine secousseCoroutine;
    
    // Variable obligatoire pour que SmoothDamp calcule l'élan de la caméra
    private Vector3 velociteActuelle = Vector3.zero;

    void Awake()
    {
        if (instance == null) instance = this;

        rotationChoisie = transform.rotation;
        angleRotationLisse = angleRotation; 
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (GameManager.instance != null && GameManager.instance.joueurActuel != null)
            {
                target = GameManager.instance.joueurActuel.transform;
            }
            return; 
        }

        // 1. Calcul de la rotation demandée
        angleRotationLisse = Mathf.Lerp(angleRotationLisse, angleRotation, Time.deltaTime * fluiditeRotation);
        Quaternion rotationPivot = Quaternion.Euler(0, angleRotationLisse, 0);
        
        Vector3 offsetTourne = rotationPivot * offset;
        Quaternion rotationFinaleCam = rotationPivot * rotationChoisie;

        // 2. Position cible
        Vector3 positionFinale = target.position + offsetTourne;

        // 3. Choix du temps de réaction (Lent si transition, très rapide si suivi normal)
        float tempsDeReactionActuel = estEnTransition ? tempsTransition : tempsSuivi;

        // --- LA MAGIE EST ICI : SmoothDamp ---
        Vector3 positionDeBase = Vector3.SmoothDamp(transform.position - offsetSecousse, positionFinale, ref velociteActuelle, tempsDeReactionActuel);
        
        transform.position = positionDeBase + offsetSecousse;


        // 4. Gestion de la rotation et de la fin de transition
        if (estEnTransition)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationFinaleCam, Time.deltaTime * 5f);
            
            // 👉 CORRECTION : On regarde si le joueur est en train de tomber ou d'avancer
            Rigidbody rbCible = target.GetComponent<Rigidbody>();
            bool joueurEnMouvement = (rbCible != null && rbCible.linearVelocity.magnitude > 0.5f);

            // Fin de la transition SI la caméra est proche OU SI le joueur a commencé à bouger
            if (Vector3.Distance(transform.position - offsetSecousse, positionFinale) < 0.5f || joueurEnMouvement) 
            {
                estEnTransition = false; 
            }
        }
        else
        {
            transform.rotation = rotationFinaleCam; 
        }
    }

    public void DemarrerTransitionDePuis(Transform positionDepart)
    {
        transform.position = positionDepart.position;
        transform.rotation = positionDepart.rotation; 
        
        // On remet la vélocité à zéro pour éviter que la caméra ne garde l'élan de sa position précédente
        velociteActuelle = Vector3.zero; 
        
        estEnTransition = true;
    }

    public void Secouer(float intensite = 0.5f, float duree = 0.2f)
    {
        if (secousseCoroutine != null) StopCoroutine(secousseCoroutine);
        secousseCoroutine = StartCoroutine(RoutineSecousse(intensite, duree));
    }

    private IEnumerator RoutineSecousse(float intensite, float duree)
    {
        float tempsEcoule = 0f;
        while (tempsEcoule < duree)
        {
            tempsEcoule += Time.deltaTime;
            float forceActuelle = Mathf.Lerp(intensite, 0f, tempsEcoule / duree);
            offsetSecousse = Random.insideUnitSphere * forceActuelle;
            yield return null; 
        }
        offsetSecousse = Vector3.zero;
    }
    public void StopperTransition()
    {
        estEnTransition = false;
    }
}