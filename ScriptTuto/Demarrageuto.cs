using UnityEngine;
using UnityEngine.InputSystem; 

public class Demarrageuto : MonoBehaviour
{
    [Header("Le tout premier panneau")]
    public GameObject panelDebut; 

    private bool enAttenteDuJoueur = false;

    void Start()
    {
        // 1. Dès la toute première frame du jeu, on fige le temps
        Time.timeScale = 0f;

        // 2. On affiche le panneau de bienvenue / instruction de base
        if (panelDebut != null)
        {
            panelDebut.SetActive(true);
        }

        // 3. On attend le tout premier tir du joueur
        enAttenteDuJoueur = true;
    }

    void Update()
    {
        if (enAttenteDuJoueur)
        {
            // --- ÉTAPE 1 : LE JOUEUR POSE LE DOIGT ---
            bool doigtPose = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) doigtPose = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) doigtPose = true;

            // Dès qu'il touche, on cache le texte pour qu'il puisse viser
            if (doigtPose && panelDebut != null)
            {
                panelDebut.SetActive(false);
            }

            // --- ÉTAPE 2 : LE JOUEUR RELÂCHE LE DOIGT ---
            bool doigtRelache = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) doigtRelache = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) doigtRelache = true;

            // Le premier tir est lâché, le jeu commence vraiment !
            if (doigtRelache)
            {
                CommencerLeJeu();
            }
        }
    }

    private void CommencerLeJeu()
    {
        enAttenteDuJoueur = false;

        // On libère la physique !
        Time.timeScale = 1f;

        // Ce script a fait son travail, il se détruit pour ne plus jamais intervenir
        Destroy(gameObject);
    }
}