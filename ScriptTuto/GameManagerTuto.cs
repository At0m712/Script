using UnityEngine;

public class GameManagerTuto : MonoBehaviour
{
    public static GameManagerTuto instance;

    public static int currentLevel;
    public static SafeInt vies = 3; 
    public static SafeInt argentTotal = 0; 
    private SafeInt scoreActuel = 0;       
    private SafeInt meilleurScore = 0;  

    [Header("Joueur et Respawn")]
    public Vector3 pointDeRespawn = new Vector3(0f, 2f, 0f);
    public GameObject joueurActuel { get; private set; }
    public Rigidbody joueurRb { get; private set; }

    private bool doitRevivre = false;

    void Awake()
    {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ChercherLeJoueur();

        currentLevel = SaveManager.instance.data.niveau;
        meilleurScore = SaveManager.instance.data.meilleurScore;
        argentTotal = SaveManager.instance.data.argentTotal;

        if (currentLevel == 1)
        {
            scoreActuel = 0;
            SaveManager.instance.data.scoreSession = 0; 
        }
        else
        {
            scoreActuel = SaveManager.instance.data.scoreSession;
        }
    }

    public void ChercherLeJoueur()
    {
        joueurActuel = GameObject.FindGameObjectWithTag("Player");
        if (joueurActuel != null)
        {
            joueurRb = joueurActuel.GetComponent<Rigidbody>();
        }
    }

    public void AjouterArgent(int montant)
    {
        if (PowerUpManager.instance != null && PowerUpManager.instance.x2Actif) montant *= 2;

        argentTotal += montant;
        SaveManager.instance.data.argentTotal = argentTotal;
    }

    public bool DepenserArgent(int montant)
    {
        if (argentTotal >= montant)
        {
            argentTotal -= montant;
            SaveManager.instance.data.argentTotal = argentTotal;
            SaveManager.instance.SauvegarderPartie();
            return true; 
        }
        return false; 
    }

    public void AjouterScore(int points)
    {
        if (PowerUpManager.instance != null && PowerUpManager.instance.x2Actif) points *= 2;

        scoreActuel += points;
        SaveManager.instance.data.scoreSession = scoreActuel;

        if (scoreActuel > meilleurScore)
        {
            meilleurScore = scoreActuel;
            SaveManager.instance.data.meilleurScore = meilleurScore;
        }
    }

    public void AjouterVie()
    {
        if (vies >= 5) 
        {
            Debug.Log("Déjà au max de vies (5) !");
            return; 
        }

        vies += 1;
        SaveManager.instance.SauvegarderPartie();
    }

    public void WinLevel()
    {
        currentLevel++;
        SaveManager.instance.data.niveau = currentLevel;
        SaveManager.instance.SauvegarderPartie(); 
        
        // Note : Le rechargement de scène a été retiré. Ce sera désormais
        // au SceneManager dédié ou au LevelManager de s'en occuper !
    }

    public bool PerdreVie()
    {
        vies--; 
        SaveManager.instance.SauvegarderPartie(); 

        if (vies > 0) 
        {
            return true; 
        }
        else
        {
            GererDefaite();
            return false; 
        }
    }

    private void GererDefaite()
    {
        // On conserve la logique de données cachées sans afficher d'UI
        if (FirebaseManager.instance != null) 
        {
            FirebaseManager.instance.EnvoyerScore(scoreActuel);
            FirebaseManager.instance.AnalyserMortJoueur(currentLevel, scoreActuel); 
        }
    }

    public void Revivre()
    {
        doitRevivre = true; 
    }

    void Update()
    {
        if (doitRevivre)
        {
            doitRevivre = false; 
            ExecuterResurrection();
        }
    }

    public void ExecuterResurrection()
    {
        vies = 1;

        if (joueurActuel == null) ChercherLeJoueur();

        if (joueurActuel != null)
        {
            if (joueurRb != null)
            {
                joueurRb.position = pointDeRespawn; 
                joueurRb.linearVelocity = Vector3.zero;  
                joueurRb.angularVelocity = Vector3.zero; 
            }

            joueurActuel.transform.position = pointDeRespawn;
            joueurActuel.transform.rotation = Quaternion.identity;
        }
        
        SaveManager.instance.SauvegarderPartie();
    }
}