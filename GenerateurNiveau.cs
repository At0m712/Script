using UnityEngine;

public class GenerateurNiveau : MonoBehaviour
{
    // 👉 NOUVEAU : Permet au MatchmakingManager de trouver ce script facilement
    public static GenerateurNiveau instance;

    [Header("Catalogue des objets")]
    public GameObject[] objetsPossibles; 
    public int nombreDObjetsAPlacer = 15; 

    [Header("Limites de la carte (Zone de Spawn)")]
    public float limiteX_Min = -20f;
    public float limiteX_Max = 20f;
    public float limiteZ_Min = 10f;  
    public float limiteZ_Max = 100f; 
    public float hauteurY = 1f;      

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        string modeChoisi = PlayerPrefs.GetString("ModeChoisi", "Normal");

        if (modeChoisi == "1v1")
        {
            // 🛑 ATTENTION : En 1v1, on ne fait RIEN dans le Start().
            // On attend que le MatchmakingManager appelle la fonction "GenererLeNiveau1v1" plus tard.
            return; 
        }

        // Mode Solo / Speedrun : On génère le niveau normalement et immédiatement
        Random.InitState((int)System.DateTime.Now.Ticks);
        PlacerLesObjets();
    }

    // 👉 NOUVEAU : Cette fonction sera activée par le Matchmaking AU BON MOMENT
    public void GenererLeNiveau1v1()
    {
        // On synchronise la seed REÇUE par Firebase
        Random.InitState(MatchmakingManager.seedDuNiveau);
        Debug.Log("🎲 Génération 1v1 lancée avec la Seed officielle : " + MatchmakingManager.seedDuNiveau);
        
        // On fait apparaître les objets synchronisés
        PlacerLesObjets();
    }

    void PlacerLesObjets()
    {
        if (objetsPossibles.Length == 0)
        {
            Debug.LogError("Attention ! La liste d'objets est vide !");
            return;
        }

        for (int i = 0; i < nombreDObjetsAPlacer; i++)
        {
            int indexAleatoire = Random.Range(0, objetsPossibles.Length);
            GameObject objetChoisi = objetsPossibles[indexAleatoire];

            float hauteurYPrefab = objetChoisi.transform.position.y;
            float positionXRandom = Random.Range(limiteX_Min, limiteX_Max);
            float positionZRandom = Random.Range(limiteZ_Min, limiteZ_Max);
            
            Vector3 positionDeSpawn = new Vector3(positionXRandom, hauteurYPrefab, positionZRandom);

            Vector3 anglesOriginels = objetChoisi.transform.rotation.eulerAngles;
            float angleYAleatoire = Random.Range(0f, 360f);
            Quaternion rotationCalculee = Quaternion.Euler(anglesOriginels.x, angleYAleatoire, anglesOriginels.z);

            Instantiate(objetChoisi, positionDeSpawn, rotationCalculee);
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 centre = new Vector3((limiteX_Min + limiteX_Max) / 2, hauteurY, (limiteZ_Min + limiteZ_Max) / 2);
        Vector3 taille = new Vector3(limiteX_Max - limiteX_Min, 0.1f, limiteZ_Max - limiteZ_Min);
        Gizmos.DrawWireCube(centre, taille);
    }
}