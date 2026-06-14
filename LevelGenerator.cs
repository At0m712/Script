using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;

    [Header("Mode Speedrun")]
    public GameObject prefabNiveauSpeedrun; 

    [Header("Modules")]
    public GameObject[] modulesPrefabs;
    public GameObject portePrefab;
    
    [Tooltip("Ces modules ne seront jamais choisis comme premier élément d'une nouvelle section")]
    public GameObject[] modulesInterditsEnPremier; 
    
    [Header("Sécurité Limite de Mort")]
    public float limiteYMinimum = -15f; 

    public Vector3 offsetPorte = Vector3.zero; 

    private float spawnZ = 10f;
    private float spawnY = 0f;

    private Queue<List<GameObject>> sectionsEnJeu = new Queue<List<GameObject>>();
    private Queue<GateController> prochainesPortes = new Queue<GateController>();
    private Queue<GameObject> portesPassees = new Queue<GameObject>();
    
    public GateController porteActuelle; 

    // --- NOUVEAU : Le dossier virtuel qui va contenir la route ---
    private GameObject conteneurNiveau; 

    void Awake() { if (instance == null) instance = this; }

    void Start()
    {
        // On crée un dossier vide "virtuel" dans la scène dès le début
        conteneurNiveau = new GameObject("Dossier_Niveau_Classique");

        string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (modeChoisi == "Speedrun")
        {
            if (prefabNiveauSpeedrun != null)
            {
                // 👇 LA CORRECTION EST ICI : On instancie l'objet et on le range dans le conteneur ! 👇
                GameObject niveauSpeedrun = Instantiate(prefabNiveauSpeedrun, Vector3.zero, Quaternion.identity);
                niveauSpeedrun.transform.SetParent(conteneurNiveau.transform);
                // 👆 ============================================================================== 👆
            }
            Debug.Log("Mode Speedrun activé : Génération aléatoire désactivée.");
            
            // Si on est en speedrun, le générateur classique n'a pas besoin de tourner
            this.enabled = false; 
        }
        else if (modeChoisi == "Normal")
        {
            CreerNouvelleSection(GameManager.currentLevel);
            CreerNouvelleSection(GameManager.currentLevel + 1);

            if (prochainesPortes.Count > 0)
            {
                porteActuelle = prochainesPortes.Dequeue();
            }
        }
    }

    public void JoueurPassePorte()
    {
        GameManager.currentLevel++;
        if (GameManager.instance != null) GameManager.instance.MettreAJourNiveau();

        if (porteActuelle != null) portesPassees.Enqueue(porteActuelle.gameObject);
        
        if (portesPassees.Count > 1) Destroy(portesPassees.Dequeue());
        
        if (prochainesPortes.Count > 0) porteActuelle = prochainesPortes.Dequeue();

        CreerNouvelleSection(GameManager.currentLevel + 1);

        if (sectionsEnJeu.Count > 2)
        {
            List<GameObject> vieilleSection = sectionsEnJeu.Dequeue();
            foreach (GameObject obj in vieilleSection)
            {
                if (obj != null) Destroy(obj);
            }
        }
    }

    void CreerNouvelleSection(int niveauDeLaSection)
    {
         List<GameObject> nouvelleSection = new List<GameObject>();
         int longueur = Mathf.Min(16, 4 + (niveauDeLaSection * 2));

         for (int i = 0; i < longueur; i++)
         {
             int randomIndex = Random.Range(0, modulesPrefabs.Length);
             GameObject prefab = modulesPrefabs[randomIndex];
             ModuleInfo info = prefab.GetComponent<ModuleInfo>();

             int tentatives = 0;
             while (tentatives < 50 && modulesPrefabs.Length > 1) 
             {
                 info = prefab.GetComponent<ModuleInfo>();
                 float hauteurPotentielle = (info != null) ? info.hauteurY : 0f;
                 bool estInterditPremier = false;
                 
                 if (i == 0 && modulesInterditsEnPremier != null && modulesInterditsEnPremier.Length > 0)
                 {
                     foreach (GameObject interdit in modulesInterditsEnPremier)
                     {
                         if (prefab == interdit) { estInterditPremier = true; break; }
                     }
                 }
                 
                 bool vaSousLaZoneDeMort = (spawnY + hauteurPotentielle < limiteYMinimum);

                 if (estInterditPremier || vaSousLaZoneDeMort)
                 {
                     randomIndex = Random.Range(0, modulesPrefabs.Length);
                     prefab = modulesPrefabs[randomIndex];
                     tentatives++;
                 }
                 else { break; }
             }

             Vector3 position = new Vector3(0, spawnY, spawnZ);
             
             // 👉 MAGIE : Les modules sont rangés dans le conteneur virtuel !
             GameObject nouveauModule = Instantiate(prefab, position, prefab.transform.rotation, conteneurNiveau.transform);
             nouvelleSection.Add(nouveauModule);

             if (info != null)
             {
                 spawnZ += info.tailleZ;
                 spawnY += info.hauteurY;
             }
         }

         Vector3 positionPorte = new Vector3(0, spawnY, spawnZ) + offsetPorte;
         
         // 👉 MAGIE : La porte aussi !
         GameObject objetPorte = Instantiate(portePrefab, positionPorte, portePrefab.transform.rotation, conteneurNiveau.transform);

         GateController controleurPorte = objetPorte.GetComponent<GateController>();
         prochainesPortes.Enqueue(controleurPorte); 

         if (SpawnManager.instance != null)
         {
             SpawnManager.instance.FaireApparaitreDans(nouvelleSection, niveauDeLaSection, controleurPorte);
         }

         sectionsEnJeu.Enqueue(nouvelleSection);
    }

    // --- NOUVELLE FONCTION POUR LE 1v1 ---
    public void StopperEtNettoyerClassique()
    {
        // 1. On éteint UNIQUEMENT ce composant LevelGenerator (Le GameManager reste allumé !)
        this.enabled = false;

        // 2. On cache le dossier virtuel et tout ce qu'il contient
        if (conteneurNiveau != null)
        {
            conteneurNiveau.SetActive(false);
        }
    }
}