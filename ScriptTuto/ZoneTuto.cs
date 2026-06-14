using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; 

public class ZoneTuto : MonoBehaviour
{
    [Header("Réglages de cette zone")]
    public GameObject panelAafficher; 
    public float tempsAvantPause = 1f; 
    public bool estLaFinDuTuto = false; 

    private bool aEteTouche = false;
    private bool enAttenteDuJoueur = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !aEteTouche)
        {
            aEteTouche = true; 
            StartCoroutine(AttendreEtMettreEnPause());
        }
    }

    private IEnumerator AttendreEtMettreEnPause()
    {
        yield return new WaitForSeconds(tempsAvantPause);

        Time.timeScale = 0f;

        if (panelAafficher != null)
        {
            panelAafficher.SetActive(true);
        }

        enAttenteDuJoueur = true;
    }

    void Update()
    {
        if (enAttenteDuJoueur)
        {
            // --- ÉTAPE 1 : LE JOUEUR POSE LE DOIGT ---
            // On cache le panneau pour le laisser voir son jeu et viser tranquillement
            bool doigtPose = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) doigtPose = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) doigtPose = true;

            if (doigtPose && panelAafficher != null)
            {
                panelAafficher.SetActive(false);
            }

            // --- ÉTAPE 2 : LE JOUEUR RELÂCHE LE DOIGT ---
            // Le tir est lâché, on enlève la pause !
            bool doigtRelache = false;
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame) doigtRelache = true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame) doigtRelache = true;

            if (doigtRelache)
            {
                ReprendreLeJeu();
            }
        }
    }

    private void ReprendreLeJeu()
    {
        enAttenteDuJoueur = false;

        // Par sécurité, on s'assure que le panneau est bien éteint
        if (panelAafficher != null)
        {
            panelAafficher.SetActive(false);
        }

        // On relance la physique et le jeu !
        Time.timeScale = 1f;

        if (estLaFinDuTuto)
        {
            PlayerPrefs.SetInt("TutoInteractifFini", 1);
            PlayerPrefs.Save();
            Debug.Log("Tutoriel validé dans la mémoire !");
        }

        Destroy(gameObject);
    }
}