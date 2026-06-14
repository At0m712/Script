using UnityEngine;
using UnityEngine.UI; // Indispensable pour contrôler les jauges
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager instance;

    [Header("États des Bonus")]
    public bool aimantActif = false;
    public bool x2Actif = false;

    [Header("Réglages")]
    public float dureeAimant = 10f;
    public float dureeX2 = 10f;

    [Header("Interface (Jauges UI)")]
    public GameObject panelJaugeAimant; // Le conteneur à afficher/cacher
    public Slider jaugeAimant;          // La barre qui se vide
    
    public GameObject panelJaugeX2;     
    public Slider jaugeX2;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // On s'assure que les jauges sont cachées au démarrage du jeu
        if (panelJaugeAimant != null) panelJaugeAimant.SetActive(false);
        if (panelJaugeX2 != null) panelJaugeX2.SetActive(false);
    }

    // --- LE MULTIPLICATEUR X2 ---
    public void ActiverX2()
    {
        StopCoroutine("RoutineX2"); // Réinitialise si on en reprend un
        StartCoroutine("RoutineX2");
    }

    private IEnumerator RoutineX2()
    {
        x2Actif = true;
        
        // --- NOUVEAU : Calcule la durée selon le niveau (Ex: Niveau 1 = 10s, Niveau 5 = 22s) ---
        int niveauActuel = SaveManager.instance.data.niveauX2;
        float dureeDynamique = 10f + (niveauActuel - 1) * 2f; 

        float tempsRestant = dureeDynamique;

        if (panelJaugeX2 != null) panelJaugeX2.SetActive(true);
        if (jaugeX2 != null) jaugeX2.maxValue = dureeDynamique; // On ajuste le max de la jauge

        while (tempsRestant > 0)
        {
            tempsRestant -= Time.deltaTime;
            if (jaugeX2 != null) jaugeX2.value = tempsRestant;
            yield return null;
        }

        x2Actif = false;
        if (panelJaugeX2 != null) panelJaugeX2.SetActive(false);
    }

    public void ActiverAimant() // Le mot "public" est obligatoire !
    {
        StopCoroutine("RoutineAimant");
        StartCoroutine("RoutineAimant");
    }

    private IEnumerator RoutineAimant()
    {
        aimantActif = true;

        // --- NOUVEAU : Même logique pour l'aimant ---
        int niveauActuel = SaveManager.instance.data.niveauAimant;
        float dureeDynamique = 10f + (niveauActuel - 1) * 2f;

        float tempsRestant = dureeDynamique;

        if (panelJaugeAimant != null) panelJaugeAimant.SetActive(true);
        if (jaugeAimant != null) jaugeAimant.maxValue = dureeDynamique;

        while (tempsRestant > 0)
        {
            tempsRestant -= Time.deltaTime;
            if (jaugeAimant != null) jaugeAimant.value = tempsRestant;
            yield return null;
        }

        aimantActif = false;
        if (panelJaugeAimant != null) panelJaugeAimant.SetActive(false);
    }
}