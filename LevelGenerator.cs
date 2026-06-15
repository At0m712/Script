using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;

    // 🚀 NOUVEAU : C'est maintenant un tableau de Prefabs ! (Mettez Size = 4 dans Unity)
    [Header("Mode Speedrun")]
    public GameObject[] prefabsNiveauxSpeedrun; 

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

    private GameObject conteneurNiveau; 

    void Awake() { if (instance == null) instance = this; }

    void Start()
    {
        conteneurNiveau = new GameObject("Dossier_Niveau_Classique");

        string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (modeChoisi == "Speedrun")
        {
            // 🚀 NOUVEAU : On récupère le niveau sélectionné dans le menu (0, 1, 2 ou 3)
            int indexNiveau = PlayerPrefs.GetInt("NiveauSpeedrunActuel", 0);

            // On vérifie que le tableau contient bien vos prefabs et que l'index existe
            if (prefabsNiveauxSpeedrun != null && prefabsNiveauxSpeedrun.Length > indexNiveau)
            {
                if (prefabsNiveauxSpeedrun[indexNiveau] != null)
                {
                    GameObject niveauSpeedrun = Instantiate(prefabsNiveauxSpeedrun[indexNiveau], Vector3.zero, Quaternion.identity);
                    niveauSpeedrun.transform.SetParent(conteneurNiveau.transform);
                }
                else
                {
                    Debug.LogError("🚨 Le prefab Speedrun numéro " + (indexNiveau + 1) + " n'est pas assigné dans le LevelGenerator !");
                }
            }
            
            Debug.Log("Mode Speedrun activé. Génération classique désactivée. Niveau chargé : " + (indexNiveau + 1));
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
             GameObject nouveauModule = Instantiate(prefab, position, prefab.transform.rotation, conteneurNiveau.transform);
             nouvelleSection.Add(nouveauModule);

             if (info != null)
             {
                 spawnZ += info.tailleZ;
                 spawnY += info.hauteurY;
             }
         }

         Vector3 positionPorte = new Vector3(0, spawnY, spawnZ) + offsetPorte;
         GameObject objetPorte = Instantiate(portePrefab, positionPorte, portePrefab.transform.rotation, conteneurNiveau.transform);
         GateController controleurPorte = objetPorte.GetComponent<GateController>();
         prochainesPortes.Enqueue(controleurPorte); 

         if (SpawnManager.instance != null)
         {
             SpawnManager.instance.FaireApparaitreDans(nouvelleSection, niveauDeLaSection, controleurPorte);
         }

         sectionsEnJeu.Enqueue(nouvelleSection);
    }

    public void StopperEtNettoyerClassique()
    {
        this.enabled = false;
        if (conteneurNiveau != null) conteneurNiveau.SetActive(false);
    }
}